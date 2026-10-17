namespace PaperFlow.Services.Interfaces
{
    using PaperFlow.Services.Models;
    public interface IResearchPaperProvider
    {
        string ProviderId { get; }

        string ProviderName { get; }

        // Simple Search
        Task<IReadOnlyList<PaperSearchResult>> SearchAsync(
            string query,
            int maxResults = 10,
            CancellationToken cancellationToken = default);

        // Advanced Search
        Task<IReadOnlyList<PaperSearchResult>> SearchAsync(
            PaperSearchCriteria criteria,
            CancellationToken cancellationToken = default);
    }
}


