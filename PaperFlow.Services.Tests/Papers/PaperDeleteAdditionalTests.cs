namespace PaperFlow.Services.Tests.Papers
{
    using NUnit.Framework;
    using PaperFlow.Services.Papers;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public class PaperDeleteAdditionalTests
{
    private PaperFlowTestDatabase db = null!;
    private PaperDeleteService service = null!;

    [SetUp]
    public void SetUp()
    {
        db = new PaperFlowTestDatabase();
        service = new PaperDeleteService(db.Context);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public async Task Count_ReturnsOnlyOwnedPapers()
    {
        await db.SeedTwoUsersAsync();
        Assert.That(await service.CountAsync(PaperFlowTestDatabase.AliceId), Is.EqualTo(1));
        Assert.That(await service.CountAsync(PaperFlowTestDatabase.BobId), Is.EqualTo(1));
    }

    [Test]
    public async Task Delete_InvalidId_ReturnsFalse()
    {
        Assert.That(await service.DeleteAsync(0, PaperFlowTestDatabase.AliceId), Is.False);
    }

    [Test]
    public void Delete_RejectsMissingUserId() =>
        Assert.ThrowsAsync<ArgumentException>(async () => await service.DeleteAsync(1, ""));

    [Test]
    public void Count_RejectsMissingUserId() =>
        Assert.ThrowsAsync<ArgumentException>(async () => await service.CountAsync(" "));

    [Test]
    public void DeleteAll_RejectsMissingUserIdBeforeProviderOperation() =>
        Assert.ThrowsAsync<ArgumentException>(async () => await service.DeleteAllAsync(""));
}

}
