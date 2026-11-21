namespace PaperFlow.Services.Interfaces.Providers
{
    using PaperFlow.Services.Models.Search;

    public interface IOpenAlexWorkLookupService
    {
        Task<PaperSearchResult?> GetByIdAsync(string workId,
            CancellationToken cancellationToken = default);
    }
}

