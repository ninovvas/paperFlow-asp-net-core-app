namespace PaperFlow.Services.Models.Admin;

public sealed class AdminUsersPageDto
{
    public string Search { get; init; } = string.Empty;
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
    public IReadOnlyList<AdminUserRowDto> Users { get; init; } = [];
}

public sealed class AdminUserRowDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool IsAdministrator { get; init; }
    public bool IsUser { get; init; }
}

public enum AdminRoleChangeResult
{
    Success,
    NotFound,
    InvalidRole,
    SelfDemotionForbidden,
    LastAdministratorForbidden,
    IdentityError
}
