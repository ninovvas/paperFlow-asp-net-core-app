namespace PaperFlow.Web.Controllers
{
    using System.Security.Claims;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using PaperFlow.Services.Interfaces;
    using PaperFlow.Web.ViewModels.Papers;

    [Authorize]
    [Route("Papers/Reading")]
    public sealed class PaperReadingController : Controller
    {
        private readonly IPaperReadingService readingService;

        public PaperReadingController(IPaperReadingService readingService)
        {
            this.readingService = readingService;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            string userId = GetUserId();

            var paper = await readingService.GetAsync(
                id,
                userId,
                cancellationToken);

            if (paper == null)
            {
                return NotFound();
            }

            return View(PaperReadingEditViewModel.FromDto(paper));
        }

        [HttpPost("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaperReadingEditViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.PaperId)
            {
                return BadRequest();
            }

            string userId = GetUserId();

            // Always obtain the actual title from the database.
            var existing = await readingService.GetAsync(id, userId, cancellationToken);

            if (existing == null)
            {
                return NotFound();
            }

            model.Title = existing.Title;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                bool updated = await readingService.UpdateAsync(model.ToDto(), userId, cancellationToken);

                if (!updated)
                {
                    return NotFound();
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                return View(model);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save the reading information.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Paper changes updated successfully.";

            return RedirectToAction("Details", "PaperDetails", new { id });
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated user ID not found.");
        }
    }
}

