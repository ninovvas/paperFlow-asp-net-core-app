namespace PaperFlow.Web.Controllers
{
    using System.Net;
    using System.Security.Claims;
    using System.Xml;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using PaperFlow.Services.Interfaces;
    using PaperFlow.Web.ViewModels.Papers;

    [Route("PaperSearch")]
    public class PaperSearchController : Controller
    {
        private readonly IPaperSearchService searchService;
        private readonly IPaperImportService importService;
        private readonly ILogger<PaperSearchController> logger;

        public PaperSearchController(
            IPaperSearchService searchService,
            IPaperImportService importService,
            ILogger<PaperSearchController> logger)
        {
            this.searchService = searchService;
            this.importService = importService;
            this.logger = logger;
        }

        // GET: /PaperSearch

        [HttpGet("")]
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            [FromQuery] PaperSearchViewModel model,
            [FromQuery] bool execute = false,
            CancellationToken cancellationToken = default)
        {
            model.AvailableSources =
                searchService.AvailableProviderIds.ToList();

            if (!execute)
            {
                return View(model);
            }

            // Empty values support clearing all checkboxes.
            model.SelectedSources = model.SelectedSources
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Revalidate after normalizing source selection.
            ModelState.Remove(nameof(model.SelectedSources));

            if (model.SelectedSources.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.SelectedSources),
                    "Select at least one research source.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.HasSearched = true;

            try
            {
                var result = await searchService.SearchAsync(
                    model.ToCriteria(),
                    model.SelectedSources,
                    cancellationToken);

                model.Results = result.Papers;
                model.FailedSources = result.FailedSources;
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(
                    string.Empty, ex.Message);
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning(ex, "Search rate limited");
                model.ErrorMessage =
                    "The research API is limiting requests.";
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Search failed");
                model.ErrorMessage =
                    "Unable to retrieve research papers.";
            }
            catch (XmlException ex)
            {
                logger.LogWarning(ex, "Invalid API XML");
                model.ErrorMessage =
                    "Invalid response from research source.";
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                model.ErrorMessage =
                    "The research request timed out.";
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
            CancellationToken cancellationToken)
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (sourceId != "arxiv")
            {
                TempData["ErrorMessage"] =
                    "Import from this source is not implemented yet.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result =
                    await importService.ImportFromArxivAsync(
                        externalId,
                        userId,
                        cancellationToken);

                if (result.AlreadyExists)
                {
                    TempData["InfoMessage"] =
                        "This paper already exists in your library.";
                }
                else if (result.Success)
                {
                    TempData["SuccessMessage"] =
                        "Paper successfully saved.";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                return RedirectToAction("Index", "Papers");
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] =
                    "Invalid paper identifier.";
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
                TempData["ErrorMessage"] =
                    "Invalid response from arXiv.";
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                TempData["ErrorMessage"] =
                    "The import request timed out.";
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Import conflict");
                TempData["ErrorMessage"] =
                    "Paper could not be saved. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

