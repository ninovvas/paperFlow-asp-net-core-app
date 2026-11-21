namespace PaperFlow.Services.Models;

public sealed class AdminDashboardDto
{
    public int TotalUsers { get; init; }
    public int Administrators { get; init; }
    public int UsersWithUserRole { get; init; }
    public int UsersWithoutRoles { get; init; }
    public int TotalPapers { get; init; }
    public int PapersAddedThisMonth { get; init; }
    public int TotalResearchSources { get; init; }
    public int EnabledResearchSources { get; init; }
    public IReadOnlyList<AdminSourceMetric> Sources { get; init; } = [];
}

public sealed record AdminSourceMetric(string Name, bool IsEnabled, int PaperCount);

