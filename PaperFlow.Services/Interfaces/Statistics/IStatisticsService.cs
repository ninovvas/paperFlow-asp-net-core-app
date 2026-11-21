namespace PaperFlow.Services.Interfaces.Statistics
{
    using PaperFlow.Services.Models.Statistics;

    public interface IStatisticsService
    {
        Task<StatisticsDashboardDto> GetDashboardAsync(
            string userId, CancellationToken cancellationToken = default);
    }

}
