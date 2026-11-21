namespace PaperFlow.Services.Interfaces.Reading
{
    using PaperFlow.Services.Models.Reading;

    public interface IPaperReadingService
    {
        Task<PaperReadingDto?> GetAsync(int paperId, string userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(PaperReadingDto data, string userId, CancellationToken cancellationToken = default);
    }
}

