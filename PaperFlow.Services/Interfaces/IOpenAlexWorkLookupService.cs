
namespace PaperFlow.Services.Interfaces
{
    using PaperFlow.Services.Models;


    public interface IOpenAlexWorkLookupService
    {
        Task<PaperSearchResult?> GetByIdAsync(string workId,
            CancellationToken cancellationToken = default);
    }
}

