namespace PaperFlow.Services.Models
{

    public sealed record MetricValue(string Label, int Value);
    public sealed record MonthlyPaperMetrics(string Month, int Added, int Completed);

    public sealed record RecentPaperMetric(int Id, string Title, DateTime CreatedOn, string Status);

    public sealed class StatisticsDashboardDto
    {
        public IReadOnlyList<RecentPaperMetric> RecentPapers { get; init; } = [];
        public int TotalPapers { get; init; }
        public int NotStarted { get; init; }
        public int Reading { get; init; }
        public int Completed { get; init; }
        public int OnHold { get; init; }
        public int Overdue { get; init; }
        public int ReadOrReading => Reading + Completed;
        public double CompletionRate => TotalPapers == 0 ? 0 : 100d * Completed / TotalPapers;
        public double? AverageRating { get; init; }
        public int AddedThisMonth { get; init; }
        public int CompletedThisMonth { get; init; }
        public IReadOnlyList<MonthlyPaperMetrics> Monthly { get; init; } = [];
        public IReadOnlyList<MetricValue> ByStatus { get; init; } = [];
        public IReadOnlyList<MetricValue> BySource { get; init; } = [];
        public IReadOnlyList<MetricValue> ByCategory { get; init; } = [];
    }

}