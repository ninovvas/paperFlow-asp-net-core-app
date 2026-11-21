namespace PaperFlow.Services.Interfaces.Admin
{

    using PaperFlow.Services.Models;

    public interface IAdminUserService
    {
        Task<AdminUsersPageDto> GetUsersAsync(string? search, int page, CancellationToken cancellationToken = default);
        Task<AdminRoleChangeResult> SetRoleAsync(string userId, string targetRole, string actingUserId,
            CancellationToken cancellationToken = default);
    }
}
