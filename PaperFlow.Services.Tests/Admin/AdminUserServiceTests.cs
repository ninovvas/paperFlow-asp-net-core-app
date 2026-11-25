

namespace PaperFlow.Services.Tests.Admin
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using PaperFlow.Common.Constants;
    using PaperFlow.Services.Admin;
    using PaperFlow.Services.Models.Admin;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class AdminUserServiceTests
{
    private PaperFlowTestDatabase db = null!;
    private UserManager<IdentityUser> users = null!;
    private AdminUserService service = null!;

    [SetUp]
    public void SetUp()
    {
        db = new PaperFlowTestDatabase();
        // The real EF Identity user store operates against the isolated InMemory context.
        var store = new UserStore<IdentityUser, IdentityRole, PaperFlow.Data.PowerFlowDbContext>(db.Context);
        users = new UserManager<IdentityUser>(store,
            Options.Create(new IdentityOptions()), new PasswordHasher<IdentityUser>(),
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null, NullLogger<UserManager<IdentityUser>>.Instance);
        service = new AdminUserService(db.Context, users);
    }

    [TearDown]
    public async Task TearDown()
    {
        users.Dispose();
        await db.DisposeAsync();
    }

    [Test]
    public async Task GetUsers_IncludesPerUserPaperCounts()
    {
        await db.SeedTwoUsersAsync();
        var page = await service.GetUsersAsync(null, 1);
        Assert.Multiple(() =>
        {
            Assert.That(page.TotalCount, Is.EqualTo(2));
            Assert.That(page.Users.Single(u => u.Id == PaperFlowTestDatabase.AliceId).PaperCount, Is.EqualTo(1));
            Assert.That(page.Users.Single(u => u.Id == PaperFlowTestDatabase.BobId).PaperCount, Is.EqualTo(1));
        });
    }

    [Test]

    public async Task GetUsers_FiltersByEmail()
    {
        await db.SeedTwoUsersAsync();

        // Verify that test users were saved.
        var allUsers = await db.Context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        Assert.That(allUsers, Has.Count.EqualTo(2));

        // Verify that EF Core InMemory can filter the email.
        int matchingUsers = await db.Context.Users
            .CountAsync(u =>
                u.Email != null &&
                u.Email.Contains("alice"));

        Assert.That(
            matchingUsers,
            Is.EqualTo(1),
            "EF Core email filtering did not find Alice.");

        // Test the real service.
        var page = await service.GetUsersAsync("alice", 1);

        Assert.Multiple(() =>
        {
            Assert.That(page.TotalCount, Is.EqualTo(1));
            Assert.That(page.Users, Has.Count.EqualTo(1));
            Assert.That(
                page.Users.Single().Id,
                Is.EqualTo(PaperFlowTestDatabase.AliceId));
        });
    }


        [Test]
    public async Task GetUsers_PageIsClampedIntoRange()
    {
        await db.SeedTwoUsersAsync();
        var page = await service.GetUsersAsync(null, 999);
        Assert.That(page.Page, Is.EqualTo(1));
    }

    [Test]
    public async Task SetRole_InvalidRole_IsRejected()
    {
        var result = await service.SetRoleAsync("any", "Owner", "admin");
        Assert.That(result, Is.EqualTo(AdminRoleChangeResult.InvalidRole));
    }

    [Test]
    public async Task SetRole_MissingUser_IsRejected()
    {
        var result = await service.SetRoleAsync("missing", RoleConstants.User, "admin");
        Assert.That(result, Is.EqualTo(AdminRoleChangeResult.NotFound));
    }

    [Test]
    public async Task SetRole_SelfDemotion_IsForbidden()
    {
        await SeedIdentityAsync();
        var result = await service.SetRoleAsync("admin", RoleConstants.User, "admin");
        Assert.That(result, Is.EqualTo(AdminRoleChangeResult.SelfDemotionForbidden));
    }

    [Test]
    public async Task SetRole_LastAdministratorCannotBeDemoted()
    {
        await SeedIdentityAsync();
        var result = await service.SetRoleAsync("admin", RoleConstants.User, "another-admin");
        Assert.That(result, Is.EqualTo(AdminRoleChangeResult.LastAdministratorForbidden));
    }

    [Test]
    public async Task SetRole_OrdinaryUserCanBecomeAdministrator()
    {
        await SeedIdentityAsync();
        var result = await service.SetRoleAsync("member", RoleConstants.Administrator, "admin");
        var member = (await users.FindByIdAsync("member"))!;
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AdminRoleChangeResult.Success));
            Assert.That(users.IsInRoleAsync(member, RoleConstants.Administrator).GetAwaiter().GetResult(), Is.True);
            Assert.That(users.IsInRoleAsync(member, RoleConstants.User).GetAwaiter().GetResult(), Is.False);
        });
    }

    private async Task SeedIdentityAsync()
    {
        db.Context.Roles.AddRange(
            new IdentityRole { Id = "role-admin", Name = RoleConstants.Administrator, NormalizedName = "ADMINISTRATOR" },
            new IdentityRole { Id = "role-user", Name = RoleConstants.User, NormalizedName = "USER" });
        db.Context.Users.AddRange(
            new IdentityUser { Id = "admin", UserName = "admin@paperflow.test", NormalizedUserName = "ADMIN@EXAMPLE.TEST", Email = "admin@paperflow.test" },
            new IdentityUser { Id = "member", UserName = "member@paperflow.test", NormalizedUserName = "MEMBER@EXAMPLE.TEST", Email = "member@paperflow.test" });
        db.Context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = "admin", RoleId = "role-admin" },
            new IdentityUserRole<string> { UserId = "member", RoleId = "role-user" });
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();
    }
}
}
