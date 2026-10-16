
using System.Net;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PaperFlow.Services.Interfaces;
using PaperFlow.Services.Models;

namespace PaperFlow.Web.Controllers
{
    [Authorize]
    [Route("ImportTest")]
    public class ImportTestController : Controller
    {
        private readonly IPaperImportService importService;
        private readonly ILogger<ImportTestController> logger;

        public ImportTestController(
            IPaperImportService importService,
            ILogger<ImportTestController> logger)
        {
            this.importService = importService;
            this.logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("Import")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(
            [FromForm] string arxivId,
            CancellationToken cancellationToken)
        {
            string? userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            try
            {
                PaperImportResult result =
                    await importService.ImportFromArxivAsync(
                        arxivId,
                        userId,
                        cancellationToken);

                if (result.AlreadyExists)
                {
                    return Conflict(result);
                }

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode ==
                      HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning(
                    "arXiv rate limit exceeded: {Message}",
                    ex.Message);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    "arXiv is temporarily limiting API requests. " +
                    "Please try again later.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(
                    ex,
                    "arXiv lookup failed.");

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "Unable to retrieve paper from arXiv.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    "The arXiv request timed out.");
            }
            catch (System.Xml.XmlException ex)
            {
                logger.LogWarning(
                    ex,
                    "Invalid XML returned by arXiv.");

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "Invalid arXiv response.");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    ex,
                    "Paper import failed.");

                return Conflict(
                    "The paper could not be imported. " +
                    "Please check whether it already exists.");
            }
        }
    }
}
