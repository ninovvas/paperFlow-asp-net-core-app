namespace PaperFlow.Services.Interfaces.Papers
{
    using PaperFlow.Services.Models.Papers;

    public interface IPaperDetailsService
    {
        Task<PaperDetailsDto?> GetByIdAsync(
            int paperId,
            string userId,
            CancellationToken cancellationToken = default);
    }
}

