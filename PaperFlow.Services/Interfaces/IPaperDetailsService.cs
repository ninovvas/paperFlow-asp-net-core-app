
namespace PaperFlow.Services.Interfaces
{
    using PaperFlow.Services.Models;
    public interface IPaperDetailsService
    {
        Task<PaperDetailsDto?> GetByIdAsync(
            int paperId,
            string userId,
            CancellationToken cancellationToken = default);
    }
}

