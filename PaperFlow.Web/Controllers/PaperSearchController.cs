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
        private readonly IMemoryCache cache;
        private readonly ILogger<PaperSearchController> logger;

        public PaperSearchController(
            IPaperSearchService searchService,
            IPaperImportService importService,
            IMemoryCache cache,
            ILogger<PaperSearchController> logger)
        {
            this.searchService = searchService;
            this.importService = importService;
            this.cache = cache;
            this.logger = logger;
        }

        // GET: /PaperSearch or /PaperSearch?execute=true&...
        // GET: /PaperSearch?searchId=<token> restores cached results.
        [HttpGet("")]
        [AllowAnonymous]
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
                TempData["ErrorMessage"] = "Search results have expired. Please search again.";
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
                TempData["ErrorMessage"] = "Paper could not be saved. Please try again.";
            }

            return BackToSearch();
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
