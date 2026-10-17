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
    public class PapersController : Controller
    {
        private readonly PowerFlowDbContext context;

        public PapersController(PowerFlowDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            string userId = GetUserId();

            var papers = await context.Papers
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedOn)
                .ToListAsync(cancellationToken);

            return View(papers);
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

