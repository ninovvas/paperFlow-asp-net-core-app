namespace PaperFlow.Web.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using PaperFlow.Common.Constants;
    using PaperFlow.Services.Interfaces.Admin;

    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Administrator)]
    public sealed class DashboardController(IAdminDashboardService dashboardService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var dashboard = await dashboardService.GetDashboardAsync(cancellationToken);
            return View(dashboard);
        }
    }
}

