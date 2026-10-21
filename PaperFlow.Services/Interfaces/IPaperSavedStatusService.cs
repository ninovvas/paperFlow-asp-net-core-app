namespace PaperFlow.Services.Interfaces
{
    public interface IPaperSavedStatusService
    {
        Task<HashSet<string>> GetSavedArxivIdsAsync(
            string userId,
            IReadOnlyCollection<string> arxivIds,
            CancellationToken cancellationToken = default);
    }
}

