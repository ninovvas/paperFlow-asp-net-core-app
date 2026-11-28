namespace PaperFlow.Services.Tests.Papers
{
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Services.Papers;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class PaperDeleteBoundaryCoverageTests
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
    public async Task Delete_NonexistentId_ReturnsFalse()
    {
        await db.SeedTwoUsersAsync();
        Assert.That(await service.DeleteAsync(int.MaxValue, PaperFlowTestDatabase.AliceId), Is.False);
        Assert.That(await db.Context.Papers.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Count_UnknownUser_ReturnsZero()
    {
        await db.SeedTwoUsersAsync();
        Assert.That(await service.CountAsync("unknown-user"), Is.Zero);
    }

    [TestCase(" ")]
    [TestCase("")]
    public void DeleteAll_EmptyUserId_RejectsBeforeDatabaseCall(string userId)
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await service.DeleteAllAsync(userId));
    }

    [Test]
    public async Task Delete_OwnerDoesNotDeleteForeignPaper()
    {
        await db.SeedTwoUsersAsync();
        Assert.That(await service.DeleteAsync(db.BobPaperId, PaperFlowTestDatabase.AliceId), Is.False);
        Assert.That(await service.CountAsync(PaperFlowTestDatabase.BobId), Is.EqualTo(1));
    }
}
}

