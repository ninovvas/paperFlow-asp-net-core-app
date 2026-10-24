namespace PaperFlow.Services
{

    using Microsoft.EntityFrameworkCore;

    using PaperFlow.Data;
    using PaperFlow.Common;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;
    public sealed class PaperReadingService : IPaperReadingService
    {
        private readonly PowerFlowDbContext context;

        private static readonly HashSet<string> AllowedStatuses =
            new(StringComparer.Ordinal)
            {
                "NotStarted",
                "WantToRead",
                "Reading",
                "Completed",
                "OnHold"
            };

        public PaperReadingService(PowerFlowDbContext context)
        {
            this.context = context;
        }

        public async Task<PaperReadingDto?> GetAsync(
            int paperId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (paperId <= 0 ||
                string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await context.Papers
                .AsNoTracking()
                .Where(p =>
                    p.Id == paperId &&
                    p.UserId == userId)
                .Select(p => new PaperReadingDto
                {
                    PaperId = p.Id,
                    Title = p.Title,
                    Status = p.Status.ToString(),
                    DueDate = p.DueDate,
                    Rating = p.Rating,
                    CurrentPage = p.CurrentPage,
                    TotalPages = p.TotalPages,
                    Notes = p.Notes,
                    StartedReadingOn = p.StartedReadingOn,
                    CompletedOn = p.CompletedOn
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> UpdateAsync(
            PaperReadingDto data,
            string userId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.", nameof(userId));
            }

            if (!AllowedStatuses.Contains(data.Status))
            {
                throw new ArgumentException("Invalid reading status.");
            }

            if (data.Rating is < DataConstants.Paper.MinRating or > DataConstants.Paper.MaxRating)
            {
                throw new ArgumentException("Rating must be between 1 and 5.");
            }

            if (data.CurrentPage < DataConstants.Paper.MinCurrentPage ||
                data.CurrentPage > DataConstants.Paper.MaxPages ||
                data.TotalPages is < DataConstants.Paper.MinPages or > DataConstants.Paper.MaxPages ||
                (data.TotalPages.HasValue &&
                 data.CurrentPage > data.TotalPages.Value) ||
                (!data.TotalPages.HasValue &&
                 data.CurrentPage > 0))
            {
                throw new ArgumentException("Invalid reading progress.");
            }

            if (data.Notes?.Length > DataConstants.Paper.NotesMaxLength)
            {
                throw new ArgumentException("Notes exceed the maximum length.");
            }

            var paper = await context.Papers
                .FirstOrDefaultAsync(p =>
                    p.Id == data.PaperId &&
                    p.UserId == userId,
                    cancellationToken);

            if (paper == null)
            {
                return false;
            }


            var newStatus = ParseStatus(paper.Status, data.Status);

            DateTime now = DateTime.UtcNow;

            paper.Status = newStatus;

            paper.DueDate = data.DueDate;
            paper.Rating = data.Rating;
            
            paper.CurrentPage = data.Status == "Completed"
                ? data.TotalPages ?? 0
                : data.CurrentPage;
            paper.TotalPages = data.TotalPages;
            paper.Notes = data.Notes?.Trim();


            if (data.Status == "Completed")
            {
                paper.StartedReadingOn ??= now;
                paper.CompletedOn ??= now;
            }
            else
            {
                paper.CompletedOn = null;

                if (data.Status == "Reading" && paper.StartedReadingOn == null)
                {
                    paper.StartedReadingOn = now;
                }
            }

            paper.ModifiedOn = now;

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }

        private static TStatus ParseStatus<TStatus>(
            TStatus existingValue,
            string status)
            where TStatus : struct, Enum
        {
            if (!Enum.TryParse<TStatus>(
                status,
                ignoreCase: false,
                out var parsedStatus) ||
                !Enum.IsDefined(parsedStatus))
            {
                throw new ArgumentException($"Status '{status}' is not defined " + "in the Paper status enum.");
            }

            return parsedStatus;
        }
    }
}

