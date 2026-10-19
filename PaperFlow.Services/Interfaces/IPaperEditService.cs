
namespace PaperFlow.Services.Interfaces
{
    using PaperFlow.Services.Models;
    public interface IPaperEditService
    {
        Task<PaperEditDto?> GetAsync(int id, string userId, CancellationToken cancellationToken = default);
        Task<PaperEditResult> UpdateAsync(PaperEditDto data, string userId, CancellationToken cancellationToken = default);
    }
}

