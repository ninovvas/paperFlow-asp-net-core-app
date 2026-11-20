namespace PaperFlow.Services.Interfaces
{

    using PaperFlow.Services.Models;

    public interface IAdminDashboardService
    {
        Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    }
}

