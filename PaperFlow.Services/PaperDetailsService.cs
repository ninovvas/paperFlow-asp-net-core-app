namespace PaperFlow.Services
{

    using Microsoft.EntityFrameworkCore;

    using PaperFlow.Data;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;
    public sealed class PaperDetailsService
        : IPaperDetailsService
    {
        private readonly PowerFlowDbContext context;

        public PaperDetailsService(PowerFlowDbContext context)
        {
            this.context = context;
        }

        public async Task<PaperDetailsDto?> GetByIdAsync(
            int paperId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (paperId <= 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException(
                    "Authenticated user ID is required.",
                    nameof(userId));
            }

            var paper = await context.Papers
                .AsNoTracking()
                .Where(p =>
                    p.Id == paperId &&
                    p.UserId == userId)
                .Select(p => new PaperDetailsDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    PublicationDate = p.PublicationDate,
                    Doi = p.Doi,
                    ExternalId = p.ExternalId,
                    PublicationUrl = p.PublicationUrl,
                    PdfUrl = p.PdfUrl,
                    LocalPdfPath = p.LocalPdfPath,

                    ResearchSourceName =
                        p.ResearchSource != null
                            ? p.ResearchSource.Name
                            : null,

                    ReadingStatus = p.Status.ToString(),
                    DueDate = p.DueDate,
                    StartedReadingOn = p.StartedReadingOn,
                    CompletedOn = p.CompletedOn,
                    Rating = p.Rating,
                    TotalPages = p.TotalPages,
                    CurrentPage = p.CurrentPage,
                    Notes = p.Notes,
                    CreatedOn = p.CreatedOn,
                    ModifiedOn = p.ModifiedOn,

                    Authors = p.PaperAuthors
                        .OrderBy(pa => pa.AuthorOrder)
                        .Select(pa => new PaperAuthorDto
                        {
                            Id = pa.AuthorId,
                            FullName = pa.Author.FullName,
                            Orcid = pa.Author.Orcid,
                            AuthorOrder = pa.AuthorOrder
                        })
                        .ToList(),

                    Categories = p.PaperCategories
                        .OrderBy(pc => pc.Category.Name)
                        .Select(pc => pc.Category.Name)
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            return paper;
        }
    }
}

