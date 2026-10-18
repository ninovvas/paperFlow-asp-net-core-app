namespace PaperFlow.Web.Controllers
{
    using System.Security.Claims;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;

    [Authorize]
    [Route("Papers/Details")]
    public class PaperDetailsController : Controller
    {
        private readonly IPaperDetailsService detailsService;
        private readonly ILogger<PaperDetailsController> logger;

        public PaperDetailsController(
            IPaperDetailsService detailsService,
            ILogger<PaperDetailsController> logger)
        {
            this.detailsService = detailsService;
            this.logger = logger;
        }

        // GET: /Papers/Details/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            string? userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            PaperDetailsDto? paper =
                await detailsService.GetByIdAsync(
                    id,
                    userId,
                    cancellationToken);

            if (paper == null)
            {
                logger.LogInformation(
                    "Paper details not found for ID {PaperId}.",
                    id);

                return NotFound();
            }

            return View(
                "~/Views/Papers/Details.cshtml",
                paper);
        }
    }
}

