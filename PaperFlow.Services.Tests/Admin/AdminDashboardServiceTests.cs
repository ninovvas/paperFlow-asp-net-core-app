namespace PaperFlow.Services.Tests.Admin
{


using Microsoft.AspNetCore.Identity;
using NUnit.Framework;
using PaperFlow.Common.Constants;
using PaperFlow.Data.Models;
using PaperFlow.Services.Admin;
using PaperFlow.Services.Tests.Infrastructure;

[TestFixture]
public class AdminDashboardServiceTests
{
    private PaperFlowTestDatabase db = null!;

    [SetUp]
    public void SetUp() => db = new PaperFlowTestDatabase();

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public async Task EmptyDatabase_ReturnsZeroMetrics()
    {
        var result = await new AdminDashboardService(db.Context).GetDashboardAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalUsers, Is.Zero);
            Assert.That(result.Administrators, Is.Zero);
            Assert.That(result.UsersWithoutRoles, Is.Zero);
            Assert.That(result.TotalPapers, Is.Zero);
            Assert.That(result.TotalResearchSources, Is.Zero);
        });
    }

    [Test]
    public async Task Dashboard_CountsUsersRolesPapersAndSources()
    {
        await db.SeedTwoUsersAsync();
        var adminRole = new IdentityRole(RoleConstants.Administrator) { Id = "role-admin" };
        var userRole = new IdentityRole(RoleConstants.User) { Id = "role-user" };
        db.Context.Roles.AddRange(adminRole, userRole);
        db.Context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = PaperFlowTestDatabase.AliceId, RoleId = adminRole.Id },
            new IdentityUserRole<string> { UserId = PaperFlowTestDatabase.BobId, RoleId = userRole.Id });
        db.Context.ResearchSources.AddRange(
            new ResearchSource { Name = "Crossref", IsEnabled = true },
            new ResearchSource { Name = "OpenAlex", IsEnabled = false });
        await db.Context.SaveChangesAsync();

        var result = await new AdminDashboardService(db.Context).GetDashboardAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalUsers, Is.EqualTo(2));
            Assert.That(result.Administrators, Is.EqualTo(1));
            Assert.That(result.UsersWithUserRole, Is.EqualTo(1));
            Assert.That(result.UsersWithoutRoles, Is.Zero);
            Assert.That(result.TotalPapers, Is.EqualTo(2));
            Assert.That(result.TotalResearchSources, Is.EqualTo(2));
            Assert.That(result.EnabledResearchSources, Is.EqualTo(1));
            Assert.That(result.Sources.Select(s => s.Name), Is.EqualTo(new[] { "Crossref", "OpenAlex" }));
        });
    }

    [Test]
    public async Task Dashboard_UsersWithoutRolesAreCounted()
    {
        await db.SeedTwoUsersAsync();
        var result = await new AdminDashboardService(db.Context).GetDashboardAsync();
        Assert.That(result.UsersWithoutRoles, Is.EqualTo(2));
    }
}
}
