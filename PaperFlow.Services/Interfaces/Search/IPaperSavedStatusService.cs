namespace PaperFlow.Services.Interfaces.Search
{
    public interface IPaperSavedStatusService
    {
        Task<HashSet<string>> GetSavedArxivIdsAsync(
            string userId,
            IReadOnlyCollection<string> arxivIds,
            CancellationToken cancellationToken = default);
        Task<HashSet<string>> GetSavedOpenAlexIdsAsync(
            string userId, IReadOnlyCollection<Models.PaperSearchResult> papers,
            CancellationToken cancellationToken = default);

        Task<HashSet<string>> GetSavedCrossrefIdsAsync(
            string userId, IReadOnlyCollection<string> dois,
            CancellationToken cancellationToken = default);
    }
}