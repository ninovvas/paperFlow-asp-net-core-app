namespace PaperFlow.Services.Search
{

    using Microsoft.Extensions.Logging;
    using PaperFlow.Services.Interfaces.Admin;
    using PaperFlow.Services.Interfaces.Providers;
    using PaperFlow.Services.Interfaces.Search;
    using PaperFlow.Services.Models;

    public class PaperSearchService : IPaperSearchService
    {
        private readonly IReadOnlyDictionary<
            string, IResearchPaperProvider> providers;

        private readonly ILogger<PaperSearchService> logger;
        private readonly IOpenAlexAbstractService openAlexAbstracts;
        private readonly IResearchSourceManagementService sourceManagement;

        public IReadOnlyList<string> AvailableProviderIds =>
            providers.Keys.OrderBy(x => x).ToList();

        public PaperSearchService(
            IEnumerable<IResearchPaperProvider> providers,
            ILogger<PaperSearchService> logger,
            IOpenAlexAbstractService openAlexAbstracts,
            IResearchSourceManagementService sourceManagement)
        {
            this.logger = logger;
            this.openAlexAbstracts = openAlexAbstracts;
            this.sourceManagement = sourceManagement;

            this.providers = providers.ToDictionary(
                p => p.ProviderId,
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IReadOnlyList<string>> GetEnabledProviderIdsAsync(
            CancellationToken cancellationToken = default)
        {
            var enabled = await sourceManagement.GetEnabledProviderIdsAsync(cancellationToken);
            return enabled.Where(providers.ContainsKey).OrderBy(x => x).ToArray();
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

            // Always read the current database status: cached or forged selections
            // must not bypass disabled providers.
            var enabledIds = await GetEnabledProviderIdsAsync(cancellationToken);

            // "all" means every currently enabled provider.
            if (sourceIds.Any(id =>
                id.Equals("all", StringComparison.OrdinalIgnoreCase)))
            {
                sourceIds = enabledIds.ToList();
            }

            if (sourceIds.Count == 0)
            {
                throw new ArgumentException(
                    "Select at least one research source.");
            }

            if (sourceIds.Any(id => !enabledIds.Contains(id,
                StringComparer.OrdinalIgnoreCase)))
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

                    var page = papers.ToList();
                    if (provider.ProviderId.Equals("crossref", StringComparison.OrdinalIgnoreCase))
                    {
                        // Enrichment is optional and bounded to keep searches responsive.
                        var missing = page.Where(p =>
                            string.IsNullOrWhiteSpace(p.Abstract) &&
                            !string.IsNullOrWhiteSpace(p.Doi)).Take(6).ToList();
                        using var gate = new SemaphoreSlim(3);
                        await Task.WhenAll(missing.Select(async paper =>
                        {
                            await gate.WaitAsync(cancellationToken);
                            try
                            {
                                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                                    cancellationToken);
                                timeout.CancelAfter(TimeSpan.FromSeconds(7));
                                string? text = await openAlexAbstracts.GetAbstractByDoiAsync(
                                    paper.Doi!, timeout.Token);
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    paper.Abstract = text;
                                    paper.AbstractSource = "OpenAlex";
                                }
                            }
                            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                            {
                                // Skip enrichment if the secondary provider is slow.
                            }
                            catch (HttpRequestException ex)
                            {
                                logger.LogWarning(ex, "Optional OpenAlex abstract lookup failed");
                            }
                            finally
                            {
                                gate.Release();
                            }
                        }));
                    }
                    result.Papers.AddRange(page);
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

                    string reason = ex is HttpRequestException httpEx &&
                        httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                        ? " (rate limit reached)"
                        : ex is HttpRequestException serverEx &&
                          serverEx.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                            ? " (temporarily unavailable)"
                            : " (request failed)";
                    result.FailedSources.Add(provider.ProviderName + reason);
                }
            }

            // One publication can be returned by multiple sources.
            // DOI is the preferred cross-source identifier.
            result.Papers = result.Papers
                .GroupBy(GetDeduplicationKey,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(p =>
                    !string.IsNullOrWhiteSpace(p.Abstract)).First())
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

