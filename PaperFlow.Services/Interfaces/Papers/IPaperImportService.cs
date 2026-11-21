namespace PaperFlow.Services.Interfaces.Papers
{
    using PaperFlow.Services.Models.Papers;

    public interface IPaperImportService
    {
        Task<PaperImportResult> ImportFromArxivAsync(
            string arxivId,
            string userId,
            CancellationToken cancellationToken = default);
        Task<PaperImportResult> ImportFromCrossrefAsync(
            string doi, string userId, CancellationToken cancellationToken = default);

        Task<PaperImportResult> ImportFromOpenAlexAsync(
            string workId, string userId, CancellationToken cancellationToken = default);
    }
}

