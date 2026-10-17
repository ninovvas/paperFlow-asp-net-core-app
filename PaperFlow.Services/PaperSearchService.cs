namespace PaperFlow.Services
{

    using Microsoft.Extensions.Logging;

    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;
    public class PaperSearchService : IPaperSearchService
    {
        private readonly IReadOnlyDictionary<
            string, IResearchPaperProvider> providers;

        private readonly ILogger<PaperSearchService> logger;

        public IReadOnlyList<string> AvailableProviderIds =>
            providers.Keys.OrderBy(x => x).ToList();

        public PaperSearchService(
            IEnumerable<IResearchPaperProvider> providers,
            ILogger<PaperSearchService> logger)
        {
            this.logger = logger;

            this.providers = providers.ToDictionary(
                p => p.ProviderId,
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<MultiSourceSearchResult> SearchAsync(
            PaperSearchCriteria criteria,
            IReadOnlyCollection<string> selectedProviders,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(criteria);
            ArgumentNullException.ThrowIfNull(selectedProviders);

            var result = new MultiSourceSearchResult();

            var sourceIds = selectedProviders
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // "all" means every registered provider.
            if (sourceIds.Any(id =>
                id.Equals("all", StringComparison.OrdinalIgnoreCase)))
            {
                sourceIds = AvailableProviderIds.ToList();
            }

            if (sourceIds.Count == 0)
            {
                throw new ArgumentException(
                    "Select at least one research source.");
            }

            if (sourceIds.Any(id => !providers.ContainsKey(id)))
            {
                throw new ArgumentException(
                    "One or more selected sources are unavailable.");
            }

            foreach (string sourceId in sourceIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var provider = providers[sourceId];

                try
                {
                    var papers = await provider.SearchAsync(
                        criteria,
                        cancellationToken);

                    result.Papers.AddRange(papers);
                    result.SuccessfulSources.Add(provider.ProviderName);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (
                    ex is HttpRequestException ||
                    ex is System.Xml.XmlException ||
                    ex is TaskCanceledException)
                {
                    logger.LogWarning(
                        ex,
                        "Search provider {Provider} failed.",
                        provider.ProviderName);

                    result.FailedSources.Add(provider.ProviderName);
                }
            }

            // One publication can be returned by multiple sources.
            // DOI is the preferred cross-source identifier.
            result.Papers = result.Papers
                .GroupBy(GetDeduplicationKey,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            return result;
        }

        private static string GetDeduplicationKey(
            PaperSearchResult paper)
        {
            if (!string.IsNullOrWhiteSpace(paper.Doi))
            {
                string doi = paper.Doi.Trim()
                    .Replace("https://doi.org/", "",
                        StringComparison.OrdinalIgnoreCase)
                    .ToLowerInvariant();

                return $"doi:{doi}";
            }

            // An external ID is unique within a source,
            // not necessarily across all providers.
            return $"{paper.SourceName}:{paper.ExternalId}";
        }
    }
}

