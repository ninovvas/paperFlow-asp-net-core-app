
namespace PaperFlow.Web.Controllers
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Data;

    [ApiController]
    [Authorize]
    [Route("api/library-search")]
    public sealed class LibrarySearchController : ControllerBase
    {
        private readonly PowerFlowDbContext context;

        public LibrarySearchController(PowerFlowDbContext context)
        {
            this.context = context;
        }

        [HttpGet("suggest")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Suggest([FromQuery] string? q, CancellationToken cancellationToken)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            string keyword = (q ?? string.Empty).Trim();
            if (keyword.Length < 2) return Ok(Array.Empty<object>());
            if (keyword.Length > 200) return BadRequest(new { error = "Search term is too long." });

            var matches = await context.Papers
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Where(p => p.Title.Contains(keyword)
                    || (p.Doi != null && p.Doi.Contains(keyword))
                    || (p.Abstract != null && p.Abstract.Contains(keyword))
                    || p.PaperAuthors.Any(pa => pa.Author.FullName.Contains(keyword))
                    || p.PaperCategories.Any(pc => pc.Category.Name.Contains(keyword)))
                .OrderByDescending(p => p.Title == keyword)
                .ThenByDescending(p => p.Title.StartsWith(keyword))
                .ThenByDescending(p => p.Title.Contains(keyword))
                .ThenByDescending(p => p.Doi != null && p.Doi.Contains(keyword))
                .ThenByDescending(p => p.PaperAuthors.Any(pa => pa.Author.FullName.Contains(keyword)))
                .ThenByDescending(p => p.PaperCategories.Any(pc => pc.Category.Name.Contains(keyword)))
                .ThenByDescending(p => p.Abstract != null && p.Abstract.Contains(keyword))
                .ThenByDescending(p => p.CreatedOn)
                .ThenByDescending(p => p.Id)
                .Select(p => new { p.Id, p.Title, p.PublicationDate, p.Status })
                .Take(5)
                .ToListAsync(cancellationToken);

            return Ok(matches.Select(p => new
            {
                id = p.Id,
                title = p.Title,
                publicationDate = p.PublicationDate?.ToString("yyyy-MM-dd"),
                status = p.Status.ToString(),
                url = Url.Action("Details", "PaperDetails", new { id = p.Id }) ?? $"/Papers/Details/{p.Id}"
            }));
        }
    }

}