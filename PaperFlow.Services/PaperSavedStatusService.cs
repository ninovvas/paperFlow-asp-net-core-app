

namespace PaperFlow.Services
{
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Data;
    using PaperFlow.Services.Interfaces;

    /// <summary>
    /// Checks whether arXiv search results were already imported by the current user.
    /// Read-only - no authenticated user; does not call arXiv or modify the database.
    /// </summary>
    public sealed class PaperSavedStatusService : IPaperSavedStatusService
    {
        private readonly PowerFlowDbContext context;

        public PaperSavedStatusService(PowerFlowDbContext context)
        {
            this.context = context;
        }

        public async Task<HashSet<string>> GetSavedArxivIdsAsync(
            string userId,
            IReadOnlyCollection<string> arxivIds,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Authenticated user ID is required.", nameof(userId));

            ArgumentNullException.ThrowIfNull(arxivIds);

            /* Limit query values to one displayed search page,
            not the entire arXiv corpus. */

            string[] ids = arxivIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (ids.Length == 0)
                return new HashSet<string>(StringComparer.Ordinal);

            /*Matches the original SQL Server schema: ResearchSources.Name and
            Papers.ResearchSourceId + ExternalId. This prevents false matches
            against external IDs belonging to a different provider. */

            int? sourceId = await context.ResearchSources
                .AsNoTracking()
                .Where(source => source.Name.ToLower() == "arxiv")
                .Select(source => (int?)source.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!sourceId.HasValue)
                return new HashSet<string>(StringComparer.Ordinal);

            var savedIds = await context.Papers
                .AsNoTracking()
                .Where(p => p.UserId == userId &&
                            p.ResearchSourceId == sourceId.Value &&
                            p.ExternalId != null &&
                            ids.Contains(p.ExternalId))
                .Select(p => p.ExternalId!)
                .ToListAsync(cancellationToken);

            return savedIds.ToHashSet(StringComparer.Ordinal);
        }
    }
}
