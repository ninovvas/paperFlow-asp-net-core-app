namespace PaperFlow.Services.Interfaces
{

    using PaperFlow.Services.Models;

    public interface IStatisticsService
    {
        Task<StatisticsDashboardDto> GetDashboardAsync(
            string userId, CancellationToken cancellationToken = default);
    }

}
