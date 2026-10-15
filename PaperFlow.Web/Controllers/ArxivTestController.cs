using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaperFlow.Services.Interfaces;

namespace PaperFlow.Web.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/arxiv")]
    public class ArxivTestController : ControllerBase
    {
        private readonly IResearchPaperProvider provider;
        private readonly ILogger<ArxivTestController> logger;

        public ArxivTestController(
            IResearchPaperProvider provider,
            ILogger<ArxivTestController> logger)
        {
            this.provider = provider;
            this.logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string query,
            [FromQuery] int maxResults = 5,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(
                    "Please provide a search query.");
            }

            try
            {
                var papers = await provider.SearchAsync(
                    query,
                    maxResults,
                    cancellationToken);

                return Ok(papers);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(
                    ex,
                    "arXiv request failed.");

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "The arXiv service is unavailable.");
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
                    "Invalid response from arXiv.");
            }
        }
    }
}
