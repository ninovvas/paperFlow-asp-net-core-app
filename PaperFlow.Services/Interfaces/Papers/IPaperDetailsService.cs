namespace PaperFlow.Services.Interfaces.Papers
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

