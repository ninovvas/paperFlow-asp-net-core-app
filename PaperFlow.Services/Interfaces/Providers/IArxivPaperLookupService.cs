namespace PaperFlow.Services.Interfaces.Providers
{
    using PaperFlow.Services.Models;
    public interface IArxivPaperLookupService
    {
        Task<PaperSearchResult?> GetByIdAsync(
            string arxivId,
            CancellationToken cancellationToken = default);
    }
}
