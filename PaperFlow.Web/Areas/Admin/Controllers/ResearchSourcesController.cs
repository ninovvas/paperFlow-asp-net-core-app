namespace PaperFlow.Web.Areas.Admin.Controllers
{

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using PaperFlow.Common.Constants;
    using PaperFlow.Services.Interfaces;

    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Administrator)]
    public sealed class ResearchSourcesController(
        IResearchSourceManagementService sourceManagement) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var sources = await sourceManagement.GetSourcesAsync(cancellationToken);
            return View(sources);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetEnabled(string providerId, bool enabled,
            CancellationToken cancellationToken)
        {
            bool changed = await sourceManagement.SetEnabledAsync(
                providerId, enabled, cancellationToken);

            TempData[changed ? "AdminSuccess" : "AdminError"] = changed
                ? $"Research source {providerId} was {(enabled ? "enabled" : "disabled")}."
                : "Unknown research source. No settings were changed.";

            return RedirectToAction(nameof(Index));
        }
    }
}

