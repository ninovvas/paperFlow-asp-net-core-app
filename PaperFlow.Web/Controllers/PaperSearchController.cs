namespace PaperFlow.Web.Controllers
{
    using System.Net;
    using System.Security.Claims;
    using System.Xml;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Web.ViewModels.Papers;

    [Route("PaperSearch")]
    public class PaperSearchController : Controller
    {
        private static readonly TimeSpan SearchLifetime = TimeSpan.FromMinutes(30);
        private readonly IPaperSearchService searchService;
        private readonly IPaperImportService importService;
        private readonly IPaperSavedStatusService savedStatusService;
        private readonly IMemoryCache cache;
        private readonly ILogger<PaperSearchController> logger;

        public PaperSearchController(
            IPaperSearchService searchService,
            IPaperImportService importService,
            IPaperSavedStatusService savedStatusService,
            IMemoryCache cache,
            ILogger<PaperSearchController> logger)
        {
            this.searchService = searchService;
            this.importService = importService;
            this.savedStatusService = savedStatusService;
            this.cache = cache;
            this.logger = logger;
        }

        // GET: /PaperSearch or /PaperSearch?execute=true&...
        // GET: /PaperSearch?searchId=<token> restores cached results.
        [HttpGet("")]
        [Authorize]
        public async Task<IActionResult> Index(
            [FromQuery] PaperSearchViewModel model,
            [FromQuery] bool execute = false,
            [FromQuery] string? searchId = null,
            CancellationToken cancellationToken = default)
        {
            // Restore the same search WITHOUT calling the external APIs again.
            if (!execute && !string.IsNullOrWhiteSpace(searchId))
            {
                if (TryGetCachedSearch(searchId, out var cached))
                {
                    cached.AvailableSources = searchService.AvailableProviderIds.ToList();
                    await RefreshSavedStatusAsync(cached, cancellationToken);
                    return View(cached);
                }

                TempData["InfoMessage"] =
                    "Your previous search has expired. Please search again.";
                return RedirectToAction(nameof(Index));
            }

            model.AvailableSources = searchService.AvailableProviderIds.ToList();
            if (!execute)
                return View(model);

            model.SelectedSources = (model.SelectedSources ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ModelState.Remove(nameof(model.SelectedSources));
            if (model.SelectedSources.Count == 0)
                ModelState.AddModelError(nameof(model.SelectedSources),
                    "Select at least one research source.");

            if (!ModelState.IsValid)
                return View(model);

            model.HasSearched = true;

            try
            {
                var result = await searchService.SearchAsync(
                    model.ToCriteria(), model.SelectedSources, cancellationToken);

                model.Results = result.Papers;
                model.FailedSources = result.FailedSources;
                // A failed provider is NOT an empty successful page.
                // Do not expose Load More after a failure (avoids repeated 429s).
                model.NextStart = model.ToCriteria().MaxResults;
                model.HasMoreResults = result.FailedSources.Count == 0 &&
                    result.Papers.Count >= model.ToCriteria().MaxResults;
                if (result.FailedSources.Count > 0 && result.Papers.Count == 0)
                    model.ErrorMessage =
                        "arXiv is temporarily unavailable or rate-limited. " +
                        "Your library is safe. Please try again later.";

                // check the database for previously imported papers.
                await RefreshSavedStatusAsync(model, cancellationToken);

                // A cache entry is tied to the authenticated user's Identity ID.
                // Anonymous users can search, but cannot save papers.
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    model.SearchId = Guid.NewGuid().ToString("N");
                    cache.Set(CacheKey(userId, model.SearchId), model,
                        new MemoryCacheEntryOptions
                        {
                            SlidingExpiration = SearchLifetime,
                        });
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning(ex, "Search rate limited");
                model.ErrorMessage = "The research API is limiting requests.";
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Search failed");
                model.ErrorMessage = "Unable to retrieve research papers.";
            }
            catch (XmlException ex)
            {
                logger.LogWarning(ex, "Invalid API XML");
                model.ErrorMessage = "Invalid response from research source.";
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                model.ErrorMessage = "The research request timed out.";
            }

            return View(model);
        }

        // POST: /PaperSearch/LoadMore
        // Offset and filters come exclusively from the authenticated user's cache.
        [HttpPost("LoadMore")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoadMore(
            [FromForm] string? searchId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchId) ||
                !TryGetCachedSearch(searchId, out var model))
            {
                TempData["ErrorMessage"] =
                    "Search results expired. Please run the search again.";
                return RedirectToAction(nameof(Index));
            }

            IActionResult Back() => RedirectToAction(nameof(Index), new { searchId });
            if (!model.HasMoreResults)
                return Back();

            int pageSize = Math.Clamp(model.ToCriteria().MaxResults, 1, 50);
            var criteria = model.ToCriteria();
            criteria.Start = model.NextStart;
            criteria.MaxResults = pageSize;

            try
            {
                var result = await searchService.SearchAsync(
                    criteria, model.SelectedSources, cancellationToken);

                // Deduplicate within and across pages, using DOI when available.
                var combined = model.Results.Concat(result.Papers)
                    .GroupBy(PaperKey, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                int added = combined.Count - model.Results.Count;
                model.Results = combined;
                model.FailedSources = result.FailedSources;

                // One request covers the same offset for each selected provider. Advance by the requested page size, NOT the de-duplicated count.
                // A partial failure must be retryable, not silently skipped.
                if (result.FailedSources.Count > 0)
                {
                    model.HasMoreResults = false;
                    TempData["ErrorMessage"] =
                        "arXiv is temporarily unavailable or rate-limited. " +
                        "Previously loaded papers are preserved. Try again later.";
                }
                else
                {
                    model.NextStart = checked(criteria.Start + pageSize);
                    model.HasMoreResults = result.Papers.Count >= pageSize;
                    if (added == 0 && result.Papers.Count == 0)
                        model.HasMoreResults = false;
                }

                await RefreshSavedStatusAsync(model, cancellationToken);
                TempData["InfoMessage"] ??=
                    added > 0 ? $"Loaded {added} additional papers." :
                        "No new papers were found on this page.";
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Load More failed");
                TempData["ErrorMessage"] =
                    ex.StatusCode == HttpStatusCode.TooManyRequests
                        ? "The research API is rate limiting requests. Try again later."
                        : "Unable to load additional papers.";
            }
            catch (XmlException ex)
            {
                logger.LogWarning(ex, "Invalid XML while loading papers");
                TempData["ErrorMessage"] = "Invalid response from research source.";
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                TempData["ErrorMessage"] = "Loading additional papers timed out.";
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Load More failed validation");
                TempData["ErrorMessage"] = "Invalid search criteria.";
            }

            return Back();
        }

        private static string PaperKey(PaperFlow.Services.Models.PaperSearchResult paper)
        {
            if (!string.IsNullOrWhiteSpace(paper.Doi))
            {
                string doi = paper.Doi.Trim()
                    .Replace("https://doi.org/", "", StringComparison.OrdinalIgnoreCase)
                    .ToLowerInvariant();
                return "doi:" + doi;
            }
            return $"{paper.SourceName}:{paper.ExternalId}";
        }

        // POST: /PaperSearch/Save
        [HttpPost("Save")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            [FromForm] string sourceId,
            [FromForm] string externalId,
            [FromForm] string? searchId,
            CancellationToken cancellationToken)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            IActionResult BackToSearch() =>
                !string.IsNullOrWhiteSpace(searchId)
                    ? RedirectToAction(nameof(Index), new { searchId })
                    : RedirectToAction(nameof(Index));

            if (string.IsNullOrWhiteSpace(searchId) ||
                !TryGetCachedSearch(searchId, out var cached))
            {
                TempData["ErrorMessage"] =
                    "Search results have expired. Please search again.";
                return BackToSearch();
            }

            if (!string.Equals(sourceId, "arxiv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Import from this source is not implemented yet.";
                return BackToSearch();
            }

            // Accept only a paper that actually occurred in this user's search.
            bool isInResults = cached.Results.Any(p =>
                string.Equals(p.SourceName, "arxiv", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.ExternalId, externalId, StringComparison.Ordinal));

            if (!isInResults)
            {
                TempData["ErrorMessage"] = "Invalid selected paper.";
                return BackToSearch();
            }

            try
            {
                var result = await importService.ImportFromArxivAsync(
                    externalId, userId, cancellationToken);

                if (result.AlreadyExists)
                {
                    TempData["InfoMessage"] =
                        "This paper already exists in your library.";
                    cached.SavedExternalIds.Add(externalId);
                }
                else if (result.Success)
                {
                    TempData["SuccessMessage"] = "Paper successfully saved.";
                    cached.SavedExternalIds.Add(externalId);
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = "Invalid paper identifier.";
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Paper import failed");
                TempData["ErrorMessage"] =
                    ex.StatusCode == HttpStatusCode.TooManyRequests
                        ? "arXiv is limiting API requests."
                        : "Unable to retrieve the selected paper.";
            }
            catch (XmlException ex)
            {
                logger.LogWarning(ex, "Invalid import XML");
                TempData["ErrorMessage"] = "Invalid response from arXiv.";
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                TempData["ErrorMessage"] = "The import request timed out.";
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Import conflict");
                TempData["ErrorMessage"] =
                    "Paper could not be saved. Please try again.";
            }

            return BackToSearch();
        }

        // POST: /PaperSearch/SaveSelected
        // Import a bounded selection only from the authenticated user's cached results.
        [HttpPost("SaveSelected")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSelected(
            [FromForm] string? searchId,
            [FromForm] List<string>? selectedPapers,
            CancellationToken cancellationToken)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            IActionResult BackToSearch() =>
                !string.IsNullOrWhiteSpace(searchId)
                    ? RedirectToAction(nameof(Index), new { searchId })
                    : RedirectToAction(nameof(Index));

            if (string.IsNullOrWhiteSpace(searchId) ||
                !TryGetCachedSearch(searchId, out var cached))
            {
                TempData["ErrorMessage"] =
                    "Search results have expired. Please search again.";
                return BackToSearch();
            }

            // Submitted keys have the format: arxiv:<externalId>.
            // Use only trusted source + ID pairs from this search session.
            var distinctKeys = (selectedPapers ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (distinctKeys.Count == 0)
            {
                TempData["InfoMessage"] = "Select at least one paper to save.";
                return BackToSearch();
            }

            const int maxBulkSelection = 50;
            if (distinctKeys.Count > maxBulkSelection)
            {
                TempData["ErrorMessage"] =
                    $"Select no more than {maxBulkSelection} papers at once.";
                return BackToSearch();
            }

            var allowed = cached.Results
                .Where(p => string.Equals(p.SourceName, "arxiv", StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(p.ExternalId))
                .GroupBy(p => "arxiv:" + p.ExternalId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().ExternalId,
                    StringComparer.Ordinal);

            // Reject the entire request if any key was injected or expired.
            if (distinctKeys.Any(key => !allowed.ContainsKey(key)))
            {
                TempData["ErrorMessage"] = "Invalid paper selection. Please try again.";
                return BackToSearch();
            }

            int imported = 0;
            int existing = 0;
            int failed = 0;
            int skipped = 0;
            var failedTitles = new List<string>();

            foreach (string key in distinctKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string externalId = allowed[key];

                if (cached.SavedExternalIds.Contains(externalId))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var result = await importService.ImportFromArxivAsync(
                        externalId, userId, cancellationToken);

                    if (result.AlreadyExists)
                    {
                        existing++;
                        cached.SavedExternalIds.Add(externalId);
                    }
                    else if (result.Success)
                    {
                        imported++;
                        cached.SavedExternalIds.Add(externalId);
                    }
                    else
                    {
                        failed++;
                        failedTitles.Add(externalId);
                    }
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid arXiv paper {ExternalId}", externalId);
                    failed++;
                    failedTitles.Add(externalId);
                }
                catch (HttpRequestException ex)
                {
                    logger.LogWarning(ex, "Bulk import failed for {ExternalId}", externalId);
                    failed++;
                    failedTitles.Add(externalId);
                    // Stop when rate limited: avoid hammering arXiv with more requests.
                    if (ex.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        TempData["InfoMessage"] =
                            "arXiv is temporarily limiting requests. Remaining papers were not processed.";
                        break;
                    }
                }
                catch (XmlException ex)
                {
                    logger.LogWarning(ex, "Invalid XML for {ExternalId}", externalId);
                    failed++;
                    failedTitles.Add(externalId);
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    failed++;
                    failedTitles.Add(externalId);
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning(ex, "Import conflict for {ExternalId}", externalId);
                    failed++;
                    failedTitles.Add(externalId);
                }
            }

            TempData["BulkImportSummary"] =
                $"Bulk import: {imported} imported, {existing} already in library, " +
                $"{skipped} previously saved in this search, {failed} failed.";

            if (failedTitles.Count > 0)
                TempData["BulkImportFailures"] =
                    "Failed arXiv IDs: " + string.Join(", ", failedTitles);

            return BackToSearch();
        }

        // The database, not the in-memory import flags, is the source of truth.
        // A newly created search and an existing cached search use the same check.
        private async Task RefreshSavedStatusAsync(
            PaperSearchViewModel model,
            CancellationToken cancellationToken)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                model.SavedExternalIds.Clear();
                return;
            }

            var arxivIds = model.Results
                .Where(p => string.Equals(p.SourceName, "arxiv",
                    StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ExternalId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            // Replacing instead of unioning the set also correctly handles deletion
            // of papers from My Library while the search cache is still alive.
            model.SavedExternalIds = await savedStatusService.GetSavedArxivIdsAsync(
                userId, arxivIds, cancellationToken);
        }

        private bool TryGetCachedSearch(
            string searchId, out PaperSearchViewModel model)
        {
            model = null!;
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            if (!cache.TryGetValue(CacheKey(userId, searchId), out PaperSearchViewModel? cached)
                || cached is null)
                return false;

            model = cached;
            return true;
        }

        private static string CacheKey(string userId, string searchId) =>
            $"PaperFlow:Search:{userId}:{searchId}";
    }
}
