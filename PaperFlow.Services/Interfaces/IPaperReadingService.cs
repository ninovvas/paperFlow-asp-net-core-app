namespace PaperFlow.Services.Interfaces
{
    using PaperFlow.Services.Models;

    public interface IPaperReadingService
    {
        Task<PaperReadingDto?> GetAsync(int paperId, string userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(PaperReadingDto data, string userId, CancellationToken cancellationToken = default);
    }
}

