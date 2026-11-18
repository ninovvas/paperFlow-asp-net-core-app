
namespace PaperFlow.Services
{
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Common.Constants;
    using PaperFlow.Common.Enums;
    using PaperFlow.Data.Models;
    using PaperFlow.Services.Models;
    using PaperFlow.Services.Providers.Crossref;

    public partial class PaperImportService
{
    public async Task<PaperImportResult> ImportFromCrossrefAsync(
        string doi, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Authenticated user is required.", nameof(userId));

        string normalizedDoi = CrossrefDoi.Normalize(doi);
        int? savedId = await context.Papers.AsNoTracking()
            .Where(p => p.UserId == userId &&
                (p.Doi == normalizedDoi ||
                 (p.ResearchSource != null && p.ResearchSource.Name == "Crossref" &&
                  p.ExternalId == normalizedDoi)))
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (savedId.HasValue)
            return PaperImportResult.Duplicate(savedId.Value);

        // Fetch metadata before opening the database transaction.
        var record = await crossrefLookup.GetByDoiAsync(normalizedDoi, cancellationToken);
        if (record == null)
            return new PaperImportResult { Message = "Publication not found on Crossref." };

        // Missing Crossref abstracts may be available from OpenAlex.
        if (string.IsNullOrWhiteSpace(record.Abstract))
        {
            try
            {
                record.Abstract = await openAlexAbstracts.GetAbstractByDoiAsync(
                    normalizedDoi, cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Abstract enrichment is optional; importing metadata must still work.
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // OpenAlex timed out; keep the Crossref record.
            }
        }

        string title = record.Title.Trim();
        if (title.Length < DataConstants.Paper.TitleMinLength ||
            title.Length > DataConstants.Paper.TitleMaxLength)
        {
            return new PaperImportResult { Message = "Crossref returned a title with an unsupported length." };
        }

        // A DOI can differ in casing or formatting; always compare normalized values.
        if (CrossrefDoi.Normalize(record.Doi!) != normalizedDoi)
            return new PaperImportResult { Message = "The returned DOI does not match the selected publication." };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await context.Users.AnyAsync(u => u.Id == userId, cancellationToken))
                throw new InvalidOperationException("Authenticated user does not exist.");

            savedId = await context.Papers.AsNoTracking()
                .Where(p => p.UserId == userId &&
                    (p.Doi == normalizedDoi ||
                     (p.ResearchSource != null && p.ResearchSource.Name == "Crossref" &&
                      p.ExternalId == normalizedDoi)))
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (savedId.HasValue)
                return PaperImportResult.Duplicate(savedId.Value);

            var source = await context.ResearchSources
                .FirstOrDefaultAsync(s => s.Name == "Crossref", cancellationToken);
            if (source == null)
            {
                source = new ResearchSource
                {
                    Name = "Crossref",
                    WebsiteUrl = "https://www.crossref.org",
                    IsEnabled = true
                };
                context.ResearchSources.Add(source);
            }

            var paper = new Paper
            {
                Title = title,
                Abstract = Limit(record.Abstract, DataConstants.Paper.AbstractMaxLength),
                PublicationDate = record.PublicationDate,
                Doi = normalizedDoi,
                ExternalId = normalizedDoi,
                PublicationUrl = Limit(record.PublicationUrl, DataConstants.Paper.UrlMaxLength),
                PdfUrl = Limit(record.PdfUrl, DataConstants.Paper.UrlMaxLength),
                ResearchSource = source,
                UserId = userId,
                Status = ReadingStatus.NotStarted,
                CurrentPage = 0,
                CreatedOn = DateTime.UtcNow
            };

            int authorOrder = 1;
            foreach (string name in record.Authors
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => Limit(n.Trim(), DataConstants.Author.NameMaxLength)!)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var author = await context.Authors
                    .FirstOrDefaultAsync(a => a.FullName == name, cancellationToken);
                if (author == null)
                {
                    author = context.Authors.Local.FirstOrDefault(a => a.FullName == name)
                        ?? new Author { FullName = name };
                    if (context.Entry(author).State == EntityState.Detached)
                        context.Authors.Add(author);
                }
                paper.PaperAuthors.Add(new PaperAuthor
                {
                    Paper = paper,
                    Author = author,
                    AuthorOrder = authorOrder++
                });
            }

            foreach (string name in record.Categories
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => Limit(n.Trim(), DataConstants.Category.NameMaxLength)!)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var category = await context.Categories
                    .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
                if (category == null)
                {
                    category = context.Categories.Local.FirstOrDefault(c => c.Name == name)
                        ?? new Category { Name = name };
                    if (context.Entry(category).State == EntityState.Detached)
                        context.Categories.Add(category);
                }
                paper.PaperCategories.Add(new PaperCategory { Paper = paper, Category = category });
            }

            context.Papers.Add(paper);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return PaperImportResult.Imported(paper.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql &&
                                           (sql.Number == 2601 || sql.Number == 2627))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException("The paper was added concurrently. Please retry.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static string? Limit(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null :
            value.Length > maxLength ? value[..maxLength] : value;
}
}
