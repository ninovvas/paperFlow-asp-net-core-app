namespace PaperFlow.Services.Interfaces
{
    using PaperFlow.Services.Models;
    public interface IArxivPaperLookupService
    {
        Task<PaperSearchResult?> GetByIdAsync(
            string arxivId,
            CancellationToken cancellationToken = default);
    }
}
