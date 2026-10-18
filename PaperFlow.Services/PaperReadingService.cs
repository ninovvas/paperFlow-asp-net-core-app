namespace PaperFlow.Services
{

    using Microsoft.EntityFrameworkCore;

    using PaperFlow.Data;
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

            if (data.Rating is < 1 or > 5)
            {
                throw new ArgumentException("Rating must be between 1 and 5.");
            }

            if (data.CurrentPage < 0 ||
                data.TotalPages is <= 0 ||
                (data.TotalPages.HasValue &&
                 data.CurrentPage > data.TotalPages.Value) ||
                (!data.TotalPages.HasValue &&
                 data.CurrentPage > 0))
            {
                throw new ArgumentException("Invalid reading progress.");
            }

            if (data.Notes?.Length > 10000)
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

  
            var newStatus = ParseStatus(paper.Status,data.Status);

            string oldStatus = paper.Status.ToString();

            DateTime now = DateTime.UtcNow;

            paper.Status = newStatus;

            paper.DueDate = data.DueDate;
            paper.Rating = data.Rating;
            paper.CurrentPage = data.CurrentPage;
            paper.TotalPages = data.TotalPages;
            paper.Notes = data.Notes?.Trim();

            
            if (oldStatus != data.Status)
            {
                if (data.Status == "Reading" &&
                    paper.StartedReadingOn == null)
                {
                    paper.StartedReadingOn = now;
                }

                if (data.Status == "Completed")
                {
                    paper.StartedReadingOn ??= now;
                    paper.CompletedOn = now;
                }
                else
                {
                    // If the paper is no longer completed, clear its completion timestamp.
                    paper.CompletedOn = null;
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

