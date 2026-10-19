
namespace PaperFlow.Services.Interfaces
{
    public interface IPaperDeleteService
    {
        Task<bool> DeleteAsync(int id, string userId, CancellationToken cancellationToken = default);

        Task<int> CountAsync(string userId, CancellationToken cancellationToken = default);

        Task<int> DeleteAllAsync(string userId, CancellationToken cancellationToken = default);
    }
}

