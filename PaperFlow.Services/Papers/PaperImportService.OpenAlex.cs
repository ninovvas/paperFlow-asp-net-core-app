namespace PaperFlow.Services
{
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Common.Constants;
    using PaperFlow.Common.Enums;
    using PaperFlow.Data.Models;
    using PaperFlow.Services.Models;
    using PaperFlow.Services.Providers.Crossref;
    using PaperFlow.Services.Providers.OpenAlex;

    public partial class PaperImportService
{
    public async Task<PaperImportResult> ImportFromOpenAlexAsync(
        string workId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Authenticated user is required.", nameof(userId));
        string id = OpenAlexWorkId.Normalize(workId);
        int? existingId = await context.Papers.AsNoTracking()
            .Where(p => p.UserId == userId && p.ResearchSource != null &&
                p.ResearchSource.Name == "OpenAlex" && p.ExternalId == id)
            .Select(p => (int?)p.Id).FirstOrDefaultAsync(cancellationToken);
        if (existingId.HasValue) return PaperImportResult.Duplicate(existingId.Value);

        var record = await openAlexLookup.GetByIdAsync(id, cancellationToken);
        if (record is null) return new PaperImportResult { Message = "Publication not found on OpenAlex." };
        if (OpenAlexWorkId.Normalize(record.ExternalId) != id)
            return new PaperImportResult { Message = "The returned OpenAlex ID does not match the selected paper." };
        string title = record.Title.Trim();
        if (title.Length < DataConstants.Paper.TitleMinLength ||
            title.Length > DataConstants.Paper.TitleMaxLength)
            return new PaperImportResult { Message = "The publication title has an unsupported length." };

        string? doi = string.IsNullOrWhiteSpace(record.Doi) ? null : CrossrefDoi.Normalize(record.Doi);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await context.Users.AnyAsync(u => u.Id == userId, cancellationToken))
                throw new InvalidOperationException("Authenticated user does not exist.");

            existingId = await context.Papers.AsNoTracking()
                .Where(p => p.UserId == userId &&
                    ((p.ResearchSource != null && p.ResearchSource.Name == "OpenAlex" && p.ExternalId == id) ||
                     (doi != null && p.Doi == doi)))
                .Select(p => (int?)p.Id).FirstOrDefaultAsync(cancellationToken);
            if (existingId.HasValue) return PaperImportResult.Duplicate(existingId.Value);

            if (doi is null)
            {
                string normalizedTitle = title.ToUpperInvariant();
                existingId = await context.Papers.AsNoTracking()
                    .Where(p => p.UserId == userId && p.Doi == null &&
                        p.Title.Trim().ToUpper() == normalizedTitle)
                    .Select(p => (int?)p.Id).FirstOrDefaultAsync(cancellationToken);
                if (existingId.HasValue) return PaperImportResult.Duplicate(existingId.Value);
            }

            var source = await context.ResearchSources
                .FirstOrDefaultAsync(s => s.Name == "OpenAlex", cancellationToken);
            if (source == null)
            {
                source = new ResearchSource
                {
                    Name = "OpenAlex",
                    WebsiteUrl = "https://openalex.org",
                    IsEnabled = true
                };
                context.ResearchSources.Add(source);
            }
            var paper = new Paper
            {
                Title = title,
                Abstract = Limit(record.Abstract, DataConstants.Paper.AbstractMaxLength),
                PublicationDate = record.PublicationDate,
                Doi = doi,
                ExternalId = id,
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
                { Paper = paper, Author = author, AuthorOrder = authorOrder++ });
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
            sql.Number is 2601 or 2627)
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
}

}

