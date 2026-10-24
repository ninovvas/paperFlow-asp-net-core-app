namespace PaperFlow.Services
{

    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;

    using PaperFlow.Common;
    using PaperFlow.Common.Enums;
    using PaperFlow.Data;
    using PaperFlow.Data.Models;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;
    using PaperFlow.Services.Providers;
    public class PaperImportService : IPaperImportService
    {
        private readonly PowerFlowDbContext context;
        private readonly IArxivPaperLookupService arxivLookup;

        public PaperImportService(
            PowerFlowDbContext context,
            IArxivPaperLookupService arxivLookup)
        {
            this.context = context;
            this.arxivLookup = arxivLookup;
        }

        public async Task<PaperImportResult> ImportFromArxivAsync(
            string arxivId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException(
                    "Authenticated user is required.",
                    nameof(userId));
            }

            string normalizedId =
                ArxivPaperLookupService.NormalizeId(arxivId);

            // First check: avoid an unnecessary API request when this user has already imported the paper.

            var existingByExternalId = await context.Papers
                .AsNoTracking()
                .Where(p =>
                    p.UserId == userId &&
                    p.ResearchSource != null &&
                    p.ResearchSource.Name == "arXiv" &&
                    p.ExternalId == normalizedId)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingByExternalId != null)
            {
                return PaperImportResult.Duplicate(
                    existingByExternalId.Id);
            }

            // Retrieve trusted publication metadata from arXiv.
       
            PaperSearchResult? result =
                await arxivLookup.GetByIdAsync(
                    normalizedId,
                    cancellationToken);

            if (result == null)
            {
                return PaperImportResult.NotFound();
            }

            if (result.Title.Length < 5 ||
                result.Title.Length > 500)
            {
                throw new InvalidOperationException(
                    "Publication title violates model validation.");
            }

            string? doi = DoiHelper.Normalize(result.Doi);
            if (doi is { Length: > DataConstants.Paper.DoiMaxLength })
            {
                throw new InvalidOperationException("DOI exceeds the supported length.");
            }

            // Begin the transaction after the API request.

            await using var transaction =
                await context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // Ensure the Identity user really exists.

                bool userExists = await context.Users
                    .AnyAsync(
                        u => u.Id == userId,
                        cancellationToken);

                if (!userExists)
                {
                    throw new InvalidOperationException(
                        "The authenticated user does not exist.");
                }

                // Check external ID and DOI inside the transaction.

                var existingPaper = await context.Papers
                    .Where(p =>
                        p.UserId == userId &&
                        (
                            (p.ResearchSource != null &&
                             p.ResearchSource.Name == "arXiv" &&
                             p.ExternalId == normalizedId)
                            ||
                            (doi != null && p.Doi == doi)
                        ))
                    .Select(p => new { p.Id })
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingPaper != null)
                {
                    return PaperImportResult.Duplicate(
                        existingPaper.Id);
                }

                if (doi == null)
                {
                    string normalizedTitle = result.Title.Trim().ToUpperInvariant();
                    var existingByTitle = await context.Papers
                        .AsNoTracking()
                        .Where(p =>
                            p.UserId == userId &&
                            p.Doi == null &&
                            p.Title.Trim().ToUpper() == normalizedTitle)
                        .Select(p => new { p.Id })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingByTitle != null)
                        return PaperImportResult.Duplicate(existingByTitle.Id);
                }

                // Source: arXiv

                ResearchSource? source = await context
                    .ResearchSources
                    .FirstOrDefaultAsync(
                        s => s.Name == "arXiv",
                        cancellationToken);

                if (source == null)
                {
                    source = new ResearchSource
                    {
                        Name = "arXiv",
                        WebsiteUrl = "https://arxiv.org",
                        IsEnabled = true
                    };

                    context.ResearchSources.Add(source);
                }

                // Construct the paper and its relationships.

                var paper = new Paper
                {
                    Title = result.Title,
                    Abstract = Truncate(result.Abstract, 10000),
                    PublicationDate = result.PublicationDate,
                    Doi = doi,
                    ExternalId = normalizedId,
                    PublicationUrl = result.PublicationUrl,
                    PdfUrl = result.PdfUrl,
                    ResearchSource = source,
                    UserId = userId,
                    Status = ReadingStatus.NotStarted,
                    CurrentPage = 0,
                    CreatedOn = DateTime.UtcNow
                };

                // Authors
                int authorOrder = 1;

                foreach (string authorName in result.Authors
                    .Select(a => a.Trim())
                    .Where(a => a.Length > 0)
                    .Distinct(StringComparer.Ordinal))
                {
                    string name = Truncate(
                        authorName, 200)!;

                    Author? author = await context.Authors
                        .FirstOrDefaultAsync(
                            a => a.FullName == name,
                            cancellationToken);

                    if (author == null)
                    {
                        author = new Author
                        {
                            FullName = name
                        };

                        context.Authors.Add(author);
                    }

                    paper.PaperAuthors.Add(new PaperAuthor
                    {
                        Paper = paper,
                        Author = author,
                        AuthorOrder = authorOrder++
                    });
                }

                // Categories

                foreach (string categoryName in result.Categories
                    .Select(c => c.Trim())
                    .Where(c => c.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string name = Truncate(
                        categoryName, 100)!;

                    Category? category = await context.Categories
                        .FirstOrDefaultAsync(
                            c => c.Name == name,
                            cancellationToken);

                    if (category == null)
                    {
                        category = new Category
                        {
                            Name = name
                        };

                        context.Categories.Add(category);
                    }

                    paper.PaperCategories.Add(new PaperCategory
                    {
                        Paper = paper,
                        Category = category
                    });
                }

                context.Papers.Add(paper);

                // Insert entire object graph atomically.

                await context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return PaperImportResult.Imported(paper.Id);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql &&
                      (sql.Number == 2601 || sql.Number == 2627))
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                /* The unique database indexes is protect 
                against concurrent duplicate imports.
                Retry should use a fresh DbContext; here
                we return a conflict and let the user retry. */

                throw new InvalidOperationException(
                    "A conflicting record was inserted concurrently. " + "Please retry the import.", ex);
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }
        private static string? Truncate(
            string? value,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }
    }
}

