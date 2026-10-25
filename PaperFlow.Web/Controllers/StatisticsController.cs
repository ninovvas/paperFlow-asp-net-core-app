namespace PaperFlow.Web.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using PaperFlow.Services.Interfaces;
    using System.Security.Claims;

    [Authorize]
    [Route("Statistics")]
    public sealed class StatisticsController(IStatisticsService statisticsService) : Controller
    {
        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();
            var dashboard = await statisticsService.GetDashboardAsync(userId, cancellationToken);
            return View(dashboard);
        }
    }
}

