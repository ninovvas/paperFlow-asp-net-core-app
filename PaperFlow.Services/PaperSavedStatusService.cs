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

        public async Task<HashSet<string>> GetSavedOpenAlexIdsAsync(
            string userId, IReadOnlyCollection<PaperFlow.Services.Models.PaperSearchResult> papers,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Authenticated user ID is required.", nameof(userId));
            ArgumentNullException.ThrowIfNull(papers);
            var works = papers.Where(p => p.SourceName.Equals("OpenAlex", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var ids = works.Select(p => p.ExternalId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var dois = works.Where(p => !string.IsNullOrWhiteSpace(p.Doi))
                .Select(p => p.Doi!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var saved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (ids.Length == 0) return saved;
            var direct = await context.Papers.AsNoTracking()
                .Where(p => p.UserId == userId && p.ResearchSource != null &&
                    p.ResearchSource.Name == "OpenAlex" && p.ExternalId != null &&
                    ids.Contains(p.ExternalId))
                .Select(p => p.ExternalId!).ToListAsync(cancellationToken);
            saved.UnionWith(direct);
            if (dois.Length > 0)
            {
                var savedDois = await context.Papers.AsNoTracking()
                    .Where(p => p.UserId == userId && p.Doi != null && dois.Contains(p.Doi))
                    .Select(p => p.Doi!).ToListAsync(cancellationToken);
                var existing = savedDois.ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var paper in works.Where(p => p.Doi != null && existing.Contains(p.Doi)))
                    saved.Add(paper.ExternalId);
            }
            return saved;
        }

        public async Task<HashSet<string>> GetSavedCrossrefIdsAsync(
            string userId, IReadOnlyCollection<string> dois,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Authenticated user ID is required.", nameof(userId));
            ArgumentNullException.ThrowIfNull(dois);

            var normalized = dois.Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (normalized.Length == 0)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var existingDois = await context.Papers.AsNoTracking()
                .Where(p => p.UserId == userId && p.Doi != null &&
                    normalized.Contains(p.Doi))
                .Select(p => p.Doi!)
                .ToListAsync(cancellationToken);

            return existingDois.ToHashSet(StringComparer.OrdinalIgnoreCase);
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
