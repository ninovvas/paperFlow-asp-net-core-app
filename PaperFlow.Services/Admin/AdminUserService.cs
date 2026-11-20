namespace PaperFlow.Services.Admin
{

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Common.Constants;
    using PaperFlow.Data;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;



    /// <summary>Reads registered users and manages only the two supported PaperFlow roles.</summary>
    public sealed class AdminUserService(
    PowerFlowDbContext context,
    UserManager<IdentityUser> userManager) : IAdminUserService
    {
        private const int PageSize = 20;

        public async Task<AdminUsersPageDto> GetUsersAsync(
            string? search, int page, CancellationToken cancellationToken = default)
        {
            string term = (search ?? string.Empty).Trim();
            if (term.Length > 100)
                term = term[..100];

            var users = context.Users.AsNoTracking().AsQueryable();
            if (term.Length != 0)
                users = users.Where(user => user.Email != null && user.Email.Contains(term));

            int total = await users.CountAsync(cancellationToken);
            int totalPages = Math.Max(1, (total + PageSize - 1) / PageSize);
            page = Math.Clamp(page, 1, totalPages);

            string? adminRoleId = await context.Roles.AsNoTracking()
                .Where(role => role.Name == RoleConstants.Administrator)
                .Select(role => role.Id)
                .FirstOrDefaultAsync(cancellationToken);
            string? regularRoleId = await context.Roles.AsNoTracking()
                .Where(role => role.Name == RoleConstants.User)
                .Select(role => role.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var rows = await users.OrderBy(user => user.Email).ThenBy(user => user.Id)
                .Skip((page - 1) * PageSize).Take(PageSize)
                .Select(user => new AdminUserRowDto
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? "(unknown)",
                    EmailConfirmed = user.EmailConfirmed,
                    IsAdministrator = adminRoleId != null && context.UserRoles.Any(
                        member => member.UserId == user.Id && member.RoleId == adminRoleId),
                    IsUser = regularRoleId != null && context.UserRoles.Any(
                        member => member.UserId == user.Id && member.RoleId == regularRoleId)
                }).ToListAsync(cancellationToken);

            return new AdminUsersPageDto
            {
                Search = term,
                Page = page,
                PageSize = PageSize,
                TotalCount = total,
                Users = rows
            };
        }

        public async Task<AdminRoleChangeResult> SetRoleAsync(
            string userId, string targetRole, string actingUserId,
            CancellationToken cancellationToken = default)
        {
            if (targetRole != RoleConstants.User && targetRole != RoleConstants.Administrator)
                return AdminRoleChangeResult.InvalidRole;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(actingUserId))
                return AdminRoleChangeResult.NotFound;

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return AdminRoleChangeResult.NotFound;

            bool isAdmin = await userManager.IsInRoleAsync(user, RoleConstants.Administrator);
            bool wantsUser = targetRole == RoleConstants.User;

            // Never allow an administrator to remove their own administrative permission.
            if (isAdmin && wantsUser && userId == actingUserId)
                return AdminRoleChangeResult.SelfDemotionForbidden;

            if (isAdmin && wantsUser)
            {
                string? adminRoleId = await context.Roles.AsNoTracking()
                    .Where(role => role.Name == RoleConstants.Administrator)
                    .Select(role => role.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                int administrators = adminRoleId is null ? 0 : await context.UserRoles.AsNoTracking()
                    .CountAsync(item => item.RoleId == adminRoleId, cancellationToken);
                if (administrators <= 1)
                    return AdminRoleChangeResult.LastAdministratorForbidden;
            }

            // Add the destination role before removing the previous one.
            // This also lets an administrator assign User to legacy accounts without a role.
            if (!await userManager.IsInRoleAsync(user, targetRole))
            {
                var added = await userManager.AddToRoleAsync(user, targetRole);
                if (!added.Succeeded)
                    return AdminRoleChangeResult.IdentityError;
            }

            string otherRole = wantsUser ? RoleConstants.Administrator : RoleConstants.User;
            if (await userManager.IsInRoleAsync(user, otherRole))
            {
                var removed = await userManager.RemoveFromRoleAsync(user, otherRole);
                if (!removed.Succeeded)
                    return AdminRoleChangeResult.IdentityError;
            }

            return AdminRoleChangeResult.Success;
        }
    }
}
