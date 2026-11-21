namespace PaperFlow.Services.Admin
{

    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Common.Constants;
    using PaperFlow.Data;
    using PaperFlow.Services.Interfaces.Admin;
    using PaperFlow.Services.Models;

    /// <summary>System-wide metrics, never used for the personal user dashboard.</summary>
    public sealed class AdminDashboardService(PowerFlowDbContext context) : IAdminDashboardService
    {
        public async Task<AdminDashboardDto> GetDashboardAsync(
            CancellationToken cancellationToken = default)
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1,
                0, 0, 0, DateTimeKind.Utc);

            var administratorsRoleId = await context.Roles.AsNoTracking()
                .Where(role => role.Name == RoleConstants.Administrator)
                .Select(role => role.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var userRoleId = await context.Roles.AsNoTracking()
                .Where(role => role.Name == RoleConstants.User)
                .Select(role => role.Id)
                .FirstOrDefaultAsync(cancellationToken);

            int administratorCount = administratorsRoleId is null ? 0 :
                await context.UserRoles.AsNoTracking()
                    .CountAsync(userRole => userRole.RoleId == administratorsRoleId, cancellationToken);

            int userRoleCount = userRoleId is null ? 0 :
                await context.UserRoles.AsNoTracking()
                    .CountAsync(userRole => userRole.RoleId == userRoleId, cancellationToken);

            int withoutRoles = await context.Users.AsNoTracking()
                .CountAsync(user => !context.UserRoles.Any(role => role.UserId == user.Id),
                    cancellationToken);

            var sourceRows = await context.ResearchSources.AsNoTracking()
                .OrderBy(source => source.Name)
                .Select(source => new AdminSourceMetric(
                    source.Name, source.IsEnabled, source.Papers.Count))
                .ToListAsync(cancellationToken);

            return new AdminDashboardDto
            {
                TotalUsers = await context.Users.CountAsync(cancellationToken),
                Administrators = administratorCount,
                UsersWithUserRole = userRoleCount,
                UsersWithoutRoles = withoutRoles,
                TotalPapers = await context.Papers.CountAsync(cancellationToken),
                PapersAddedThisMonth = await context.Papers.CountAsync(
                    paper => paper.CreatedOn >= startOfMonth, cancellationToken),
                TotalResearchSources = sourceRows.Count,
                EnabledResearchSources = sourceRows.Count(source => source.IsEnabled),
                Sources = sourceRows
            };
        }
    }
}

