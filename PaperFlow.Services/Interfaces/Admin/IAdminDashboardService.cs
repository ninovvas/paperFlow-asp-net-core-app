namespace PaperFlow.Services.Interfaces.Admin
{
    using PaperFlow.Services.Models.Admin;

    public interface IAdminDashboardService
    {
        Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    }
}

