namespace PaperFlow.Services
{
    using System.Globalization;
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Common.Enums;
    using PaperFlow.Data;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;

    public sealed class StatisticsService(PowerFlowDbContext context) : IStatisticsService
    {
        public async Task<StatisticsDashboardDto> GetDashboardAsync(
            string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            var now = DateTime.UtcNow;
            var startMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var earliestMonth = startMonth.AddMonths(-11);
            var nextMonth = startMonth.AddMonths(1);
            var today = DateOnly.FromDateTime(now);

            var papers = context.Papers.AsNoTracking().Where(p => p.UserId == userId);

            // All statistics are scoped to this Identity user, including relationship aggregations.
            var counts = await papers.GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    NotStarted = g.Count(p => p.Status == ReadingStatus.NotStarted),
                    Reading = g.Count(p => p.Status == ReadingStatus.Reading),
                    Completed = g.Count(p => p.Status == ReadingStatus.Completed),
                    Overdue = g.Count(p => p.DueDate != null && p.DueDate < today && p.Status != ReadingStatus.Completed),
                    AddedThisMonth = g.Count(p => p.CreatedOn >= startMonth && p.CreatedOn < nextMonth),
                    CompletedThisMonth = g.Count(p => p.CompletedOn != null && p.CompletedOn >= startMonth && p.CompletedOn < nextMonth),
                    AverageRating = g.Average(p => (double?)p.Rating)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var monthlyAdded = await papers
                .Where(p => p.CreatedOn >= earliestMonth && p.CreatedOn < nextMonth)
                .GroupBy(p => new { p.CreatedOn.Year, p.CreatedOn.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var monthlyCompleted = await papers
                .Where(p => p.CompletedOn != null && p.CompletedOn >= earliestMonth && p.CompletedOn < nextMonth)
                .GroupBy(p => new { p.CompletedOn!.Value.Year, p.CompletedOn.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var addedLookup = monthlyAdded.ToDictionary(x => (x.Year, x.Month), x => x.Count);
            var completedLookup = monthlyCompleted.ToDictionary(x => (x.Year, x.Month), x => x.Count);
            var months = Enumerable.Range(0, 12)
                .Select(i => earliestMonth.AddMonths(i))
                .Select(d => new MonthlyPaperMetrics(
                    d.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    addedLookup.GetValueOrDefault((d.Year, d.Month)),
                    completedLookup.GetValueOrDefault((d.Year, d.Month))))
                .ToArray();

            var statuses = await papers.GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var sources = await papers
                .GroupBy(p => p.ResearchSource == null ? "Manual" : p.ResearchSource.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            var categories = await context.PaperCategories.AsNoTracking()
                .Where(pc => pc.Paper.UserId == userId)
                .GroupBy(pc => pc.Category.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            // Last five papers, scoped to the current user. Never include other users' records.
            var recent = await papers
                .OrderByDescending(p => p.CreatedOn).ThenByDescending(p => p.Id)
                .Take(5)
                .Select(p => new { p.Id, p.Title, p.CreatedOn, p.Status })
                .ToListAsync(cancellationToken);

            return new StatisticsDashboardDto
            {
                RecentPapers = recent.Select(p => new RecentPaperMetric(p.Id, p.Title, p.CreatedOn, p.Status.ToString())).ToArray(),
                TotalPapers = counts?.Total ?? 0,
                NotStarted = counts?.NotStarted ?? 0,
                Reading = counts?.Reading ?? 0,
                Completed = counts?.Completed ?? 0,
                OnHold = statuses.FirstOrDefault(x => x.Status.ToString() == "OnHold")?.Count ?? 0,
                Overdue = counts?.Overdue ?? 0,
                AddedThisMonth = counts?.AddedThisMonth ?? 0,
                CompletedThisMonth = counts?.CompletedThisMonth ?? 0,
                AverageRating = counts?.AverageRating,
                Monthly = months,
                ByStatus = statuses.Select(x => new MetricValue(x.Status.ToString(), x.Count)).ToArray(),
                BySource = sources.Select(x => new MetricValue(x.Name ?? "Manual", x.Count)).ToArray(),
                ByCategory = categories.Select(x => new MetricValue(x.Name, x.Count)).ToArray()
            };
        }
    }

}
