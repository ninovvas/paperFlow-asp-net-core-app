namespace PaperFlow.Services
{
    using Microsoft.EntityFrameworkCore;

    using PaperFlow.Data;
    using PaperFlow.Data.Models;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;
    using PaperFlow.Common;

    public sealed class PaperEditService : IPaperEditService
    {
        private readonly PowerFlowDbContext context;

        public PaperEditService(PowerFlowDbContext context)
        {
            this.context = context;
        }

        public async Task<PaperEditDto?> GetAsync(
            int id,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(userId))
                return null;

            return await context.Papers
                .AsNoTracking()
                .Where(p => p.Id == id && p.UserId == userId)
                .Select(p => new PaperEditDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    PublicationDate = p.PublicationDate,
                    Doi = p.Doi,
                    PublicationUrl = p.PublicationUrl,
                    PdfUrl = p.PdfUrl,

                    Authors = p.PaperAuthors
                        .OrderBy(a => a.AuthorOrder)
                        .Select(a => a.Author.FullName)
                        .ToList(),

                    Categories = p.PaperCategories
                        .OrderBy(c => c.Category.Name)
                        .Select(c => c.Category.Name)
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }


        public async Task<PaperEditResult> UpdateAsync(
            PaperEditDto data,
            string userId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException(
                    "User ID is required.",
                    nameof(userId));
            }

            // Normalize values only ONCE
            string title = data.Title?.Trim() ?? string.Empty;
            string? doi = DoiHelper.Normalize(data.Doi);

            var authorNames = NormalizeList(data.Authors);
            var categoryNames = NormalizeList(data.Categories);

            bool invalidTitle =
                title.Length < DataConstants.Paper.TitleMinLength ||
                title.Length > DataConstants.Paper.TitleMaxLength;

            bool invalidMetadata =
                (data.Abstract?.Length ?? 0) > DataConstants.Paper.AbstractMaxLength ||
                (doi?.Length ?? 0) > DataConstants.Paper.DoiMaxLength;

            bool invalidUrls =
                (data.PublicationUrl?.Length ?? 0) > DataConstants.Paper.UrlMaxLength ||
                (data.PdfUrl?.Length ?? 0) > DataConstants.Paper.UrlMaxLength ||
                !ValidUrl(data.PublicationUrl) ||
                !ValidUrl(data.PdfUrl);

            bool invalidAuthors = authorNames.Any(
                name => name.Length > DataConstants.Author.NameMaxLength);
            bool invalidCategories = categoryNames.Any(
                name => name.Length > DataConstants.Category.NameMaxLength);

            if (invalidTitle || invalidMetadata || invalidUrls ||
                invalidAuthors || invalidCategories)
            {
                return new PaperEditResult(
                    PaperEditStatus.ValidationFailed,
                    "Please correct the paper metadata.");
            }


            await using var transaction =
                    await context.Database.BeginTransactionAsync(
                        cancellationToken);

            var paper = await context.Papers
                .Include(p => p.PaperAuthors)
                .Include(p => p.PaperCategories)
                .FirstOrDefaultAsync(
                    p => p.Id == data.Id && p.UserId == userId,
                    cancellationToken);

            if (paper == null)
                return new(PaperEditStatus.NotFound, "Paper not found.");

            if (doi != null)
            {
                bool duplicate = await context.Papers
                    .AsNoTracking()
                    .AnyAsync(p =>
                        p.Id != data.Id &&
                        p.UserId == userId &&
                        p.Doi == doi,
                        cancellationToken);

                if (duplicate)
                    return new(
                        PaperEditStatus.DuplicateDoi, "This DOI already exists in your library.");
            }

            string normalizedTitle = title.ToUpperInvariant();
            bool duplicateTitle = await context.Papers
                .AsNoTracking()
                .AnyAsync(p =>
                    p.Id != data.Id &&
                    p.UserId == userId &&
                    p.Title.Trim().ToUpper() == normalizedTitle,
                    cancellationToken);

            if (duplicateTitle)
            {
                return new(
                    PaperEditStatus.DuplicateTitle,
                    "A paper with this title already exists in your library.");
            }

            paper.Title = title;
            paper.Abstract = EmptyToNull(data.Abstract);
            paper.PublicationDate = data.PublicationDate;
            paper.Doi = doi;
            paper.PublicationUrl = EmptyToNull(data.PublicationUrl);
            paper.PdfUrl = EmptyToNull(data.PdfUrl);
            paper.ModifiedOn = DateTime.UtcNow;

            try
            {
                // First remove existing join relationships.
                context.PaperAuthors.RemoveRange(paper.PaperAuthors);
                context.PaperCategories.RemoveRange(paper.PaperCategories);

                await context.SaveChangesAsync(cancellationToken);

                // Rebuild author relations in the requested order.
                int order = 1;

                foreach (string name in authorNames)
                {
                    var author = context.Authors.Local
                        .FirstOrDefault(a =>
                            a.FullName.Equals(
                                name,
                                StringComparison.OrdinalIgnoreCase));

                    author ??= await context.Authors
                        .FirstOrDefaultAsync(
                            a => a.FullName == name,
                            cancellationToken);

                    if (author == null)
                    {
                        author = new Author { FullName = name };
                        context.Authors.Add(author);
                    }

                    context.PaperAuthors.Add(new PaperAuthor
                    {
                        Paper = paper,
                        Author = author,
                        AuthorOrder = order++
                    });
                }

                foreach (string name in categoryNames)
                {
                    var category = context.Categories.Local
                        .FirstOrDefault(c =>
                            c.Name.Equals(
                                name,
                                StringComparison.OrdinalIgnoreCase));

                    category ??= await context.Categories
                        .FirstOrDefaultAsync(
                            c => c.Name == name,
                            cancellationToken);

                    if (category == null)
                    {
                        category = new Category { Name = name };
                        context.Categories.Add(category);
                    }

                    context.PaperCategories.Add(new PaperCategory
                    {
                        Paper = paper,
                        Category = category
                    });
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new(
                    PaperEditStatus.Success, "Paper updated successfully.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new(
                    PaperEditStatus.Conflict,
                    "The paper could not be saved due to a database conflict.");
            }
        }

        private static List<string> NormalizeList(
            IEnumerable<string>? values)
        {
            return (values ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool ValidUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            return Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps ||
                    uri.Scheme == Uri.UriSchemeHttp);
        }
    }
}

