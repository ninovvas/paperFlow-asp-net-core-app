namespace PaperFlow.Services.Interfaces.Search
{
    using PaperFlow.Services.Models.Search;

    public interface IPaperSearchService
    {

        IReadOnlyList<string> AvailableProviderIds { get; }

        Task<MultiSourceSearchResult> SearchAsync(
            PaperSearchCriteria criteria, IReadOnlyCollection<string> selectedProviders, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetEnabledProviderIdsAsync(CancellationToken cancellationToken = default);
    }
}

