namespace PaperFlow.Web.Controllers
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    using PaperFlow.Data;
    using PaperFlow.Data.Models;
    using PaperFlow.Web.ViewModels.Papers;

    [Authorize]
    public partial class PapersController : Controller
    {
        private readonly PowerFlowDbContext context;

        public PapersController(PowerFlowDbContext context)
        {
            this.context = context;
        }

        // GET /Papers: server-side search, filtering, sorting and pagination.
        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] PaperLibraryQuery query,
            CancellationToken cancellationToken = default)
        {
            query ??= new PaperLibraryQuery();
            query.Normalize();

            string userId = GetUserId();

            // Statistics always describe the complete user's library.
            IQueryable<Paper> ownedPapers = context.Papers
                .AsNoTracking()
                .Where(p => p.UserId == userId);

            int totalPapers = await ownedPapers.CountAsync(cancellationToken);
            int readingCount = await ownedPapers.CountAsync(
                p => p.Status == PaperFlow.Common.Enums.ReadingStatus.Reading,
                cancellationToken);
            int completedCount = await ownedPapers.CountAsync(
                p => p.Status == PaperFlow.Common.Enums.ReadingStatus.Completed,
                cancellationToken);

            IQueryable<Paper> filtered = ownedPapers;

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string term = query.Search;
                filtered = filtered.Where(p =>
                    p.Title.Contains(term) ||
                    (p.Doi != null && p.Doi.Contains(term)) ||
                    p.PaperAuthors.Any(pa =>
                        pa.Author.FullName.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                if (Enum.TryParse<PaperFlow.Common.Enums.ReadingStatus>(
                    query.Status, ignoreCase: true, out var status) &&
                    Enum.IsDefined(status))
                {
                    filtered = filtered.Where(p => p.Status == status);
                    query.Status = status.ToString();
                }
                else
                {
                    query.Status = null;
                }
            }

            if (query.SourceId.HasValue)
            {
                filtered = filtered.Where(
                    p => p.ResearchSourceId == query.SourceId.Value);
            }

            filtered = query.Sort switch
            {
                "added_asc" => filtered.OrderBy(p => p.CreatedOn).ThenBy(p => p.Id),
                "title_asc" => filtered.OrderBy(p => p.Title).ThenBy(p => p.Id),
                "title_desc" => filtered.OrderByDescending(p => p.Title).ThenBy(p => p.Id),
                "published_asc" => filtered.OrderBy(p => p.PublicationDate).ThenBy(p => p.Id),
                "published_desc" => filtered.OrderByDescending(p => p.PublicationDate).ThenBy(p => p.Id),
                "status_asc" => filtered.OrderBy(p => p.Status).ThenBy(p => p.Id),
                "status_desc" => filtered.OrderByDescending(p => p.Status).ThenBy(p => p.Id),
                "rating_asc" => filtered.OrderBy(p => p.Rating).ThenBy(p => p.Id),
                "rating_desc" => filtered.OrderByDescending(p => p.Rating).ThenBy(p => p.Id),
                _ => filtered.OrderByDescending(p => p.CreatedOn).ThenByDescending(p => p.Id)
            };

            int filteredCount = await filtered.CountAsync(cancellationToken);
            int totalPages = Math.Max(1, (int)Math.Ceiling(
                filteredCount / (double)query.PageSize));
            query.Page = Math.Min(query.Page, totalPages);

            var papers = await filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // Only sources that exist in this user's own library.
            var sources = await ownedPapers
                .Where(p => p.ResearchSourceId != null && p.ResearchSource != null)
                .Select(p => new PaperLibrarySourceOption
                {
                    Id = p.ResearchSourceId!.Value,
                    Name = p.ResearchSource!.Name
                })
                .Distinct()
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            var model = new PaperLibraryViewModel
            {
                Papers = papers,
                Query = query,
                TotalPapers = totalPapers,
                ReadingCount = readingCount,
                CompletedCount = completedCount,
                FilteredCount = filteredCount,
                TotalPages = totalPages,
                Sources = sources
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PaperCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PaperCreateViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string userId = GetUserId();

            string? doi = NormalizeDoi(model.Doi);

            if (doi != null && doi.Length > 255)
            {
                ModelState.AddModelError(
                    nameof(model.Doi),
                    "DOI must not exceed 255 characters.");

                return View(model);
            }

            if (doi != null &&
                await context.Papers.AnyAsync(
                    p => p.UserId == userId && p.Doi == doi,
                    cancellationToken))
            {
                ModelState.AddModelError(
                    nameof(model.Doi),
                    "This DOI already exists in your library.");

                return View(model);
            }

            var paper = new Paper
            {
                Title = model.Title.Trim(),
                Abstract = model.Abstract?.Trim(),
                PublicationDate = model.PublicationDate,
                Doi = doi,
                PublicationUrl = model.PublicationUrl?.Trim(),
                PdfUrl = model.PdfUrl?.Trim(),
                UserId = userId,
                CreatedOn = DateTime.UtcNow
            };

            foreach (string name in ParseNames(model.Authors))
            {
                if (name.Length > 200)
                {
                    ModelState.AddModelError(
                        nameof(model.Authors),
                        "An author name exceeds 200 characters.");
                    return View(model);
                }
            }

            foreach (string name in ParseNames(model.Categories))
            {
                if (name.Length > 100)
                {
                    ModelState.AddModelError(
                        nameof(model.Categories),
                        "A category exceeds 100 characters.");
                    return View(model);
                }
            }

            int order = 1;

            foreach (string name in ParseNames(model.Authors))
            {
                var author = await context.Authors
                    .FirstOrDefaultAsync(
                        a => a.FullName == name,
                        cancellationToken);

                author ??= context.Authors.Local
                    .FirstOrDefault(a => a.FullName == name);

                if (author == null)
                {
                    author = new Author { FullName = name };
                    context.Authors.Add(author);
                }

                paper.PaperAuthors.Add(new PaperAuthor
                {
                    Author = author,
                    AuthorOrder = order++
                });
            }

            foreach (string name in ParseNames(model.Categories))
            {
                var category = await context.Categories
                    .FirstOrDefaultAsync(
                        c => c.Name == name,
                        cancellationToken);

                category ??= context.Categories.Local
                    .FirstOrDefault(c => c.Name == name);

                if (category == null)
                {
                    category = new Category { Name = name };
                    context.Categories.Add(category);
                }

                paper.PaperCategories.Add(new PaperCategory
                {
                    Category = category
                });
            }

            context.Papers.Add(paper);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Do not expose SQL Server details to the browser.
                ModelState.AddModelError(
                    string.Empty,
                    "Could not save the paper. Check whether " +
                    "it or its metadata already exists.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Paper successfully added to your library.";

            return RedirectToAction(nameof(Index));
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException(
                    "Authenticated user ID not found.");
        }

        private static List<string> ParseNames(string? value)
        {
            return (value ?? string.Empty)
                .Split(',', StringSplitOptions.TrimEntries |
                            StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? NormalizeDoi(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string doi = value.Trim();

            if (doi.StartsWith(
                "https://doi.org/",
                StringComparison.OrdinalIgnoreCase))
            {
                doi = doi["https://doi.org/".Length..];
            }

            if (doi.StartsWith(
                "doi:",
                StringComparison.OrdinalIgnoreCase))
            {
                doi = doi[4..];
            }

            return doi.Trim().ToLowerInvariant();
        }
    }
}

