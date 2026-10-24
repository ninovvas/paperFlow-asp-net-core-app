namespace PaperFlow.Web.Controllers
{
    using System.Security.Claims;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;
    using PaperFlow.Web.ViewModels.Papers;

    [Authorize]
    public partial class PapersController
    {
        // =====================================
        // GET: /Papers/Edit/1
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id,
            [FromServices] IPaperEditService editService,
            CancellationToken cancellationToken)
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var paper = await editService.GetAsync(
                id, userId, cancellationToken);

            if (paper == null)
                return NotFound();

            return View(PaperEditViewModel.FromDto(paper));
        }

        // =====================================
        // POST: /Papers/Edit/1
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            PaperEditViewModel model,
            [FromServices] IPaperEditService editService,
            CancellationToken cancellationToken)
        {
            if (id != model.Id)
                return BadRequest();

            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Confirm ownership even if validation fails.
            var existing = await editService.GetAsync(
                id, userId, cancellationToken);

            if (existing == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            PaperEditResult result = await editService.UpdateAsync(
                model.ToDto(),
                userId,
                cancellationToken);

            switch (result.Status)
            {
                case PaperEditStatus.Success:
                    TempData["SuccessMessage"] = result.Message;

                    return RedirectToAction(
                        "Details",
                        "PaperDetails",
                        new { id });

                case PaperEditStatus.NotFound:
                    return NotFound();

                case PaperEditStatus.DuplicateDoi:
                    ModelState.AddModelError(
                        nameof(model.Doi),
                        result.Message);
                    break;

                case PaperEditStatus.DuplicateTitle:
                    ModelState.AddModelError(
                        nameof(model.Title),
                        result.Message);
                    break;

                default:
                    ModelState.AddModelError(
                        string.Empty,
                        result.Message);
                    break;
            }

            return View(model);
        }

        // =====================================
        // POST: /Papers/Delete/1
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            [FromServices] IPaperDeleteService deleteService,
            CancellationToken cancellationToken)
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            bool deleted = await deleteService.DeleteAsync(
                id, userId, cancellationToken);

            if (!deleted)
                return NotFound();

            TempData["SuccessMessage"] = "Paper deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // GET: /Papers/DeleteAll
        // =====================================

        [HttpGet]
        public async Task<IActionResult> DeleteAll(
            [FromServices] IPaperDeleteService deleteService,
            CancellationToken cancellationToken)
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            ViewBag.PaperCount = await deleteService.CountAsync(
                userId, cancellationToken);

            return View();
        }

        // =====================================
        // POST: /Papers/DeleteAll
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll(
            [FromForm] string? confirmation,
            [FromServices] IPaperDeleteService deleteService,
            CancellationToken cancellationToken)
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            if (confirmation != "DELETE ALL")
            {
                ModelState.AddModelError(string.Empty, "Please type DELETE ALL to confirm.");

                ViewBag.PaperCount = await deleteService.CountAsync(
                    userId, cancellationToken);

                return View();
            }

            int count = await deleteService.DeleteAllAsync(
                userId, cancellationToken);

            TempData["SuccessMessage"] = $"{count} paper(s) deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


    }
}

