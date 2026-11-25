namespace PaperFlow.Services.Tests.Security
{
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Services.Models.Reading;
    using PaperFlow.Services.Papers;
    using PaperFlow.Services.Reading;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class PaperOwnershipTests
{
    private PaperFlowTestDatabase db = null!;

    [SetUp]
    public async Task SetUp()
    {
        db = new PaperFlowTestDatabase();
        await db.SeedTwoUsersAsync();
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public async Task Details_OwnerCanReadOwnPaper()
    {
        var result = await new PaperDetailsService(db.Context)
            .GetByIdAsync(db.AlicePaperId, PaperFlowTestDatabase.AliceId);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Does.Contain("Alice"));
    }

    [Test]
    public async Task Details_CannotReadAnotherUsersPaper()
    {
        var result = await new PaperDetailsService(db.Context)
            .GetByIdAsync(db.BobPaperId, PaperFlowTestDatabase.AliceId);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Edit_CannotLoadAnotherUsersPaper()
    {
        var result = await new PaperEditService(db.Context)
            .GetAsync(db.BobPaperId, PaperFlowTestDatabase.AliceId);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Edit_CanLoadOwnPaper()
    {
        var result = await new PaperEditService(db.Context)
            .GetAsync(db.AlicePaperId, PaperFlowTestDatabase.AliceId);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(db.AlicePaperId));
    }

    [Test]
    public async Task Reading_CannotLoadAnotherUsersProgress()
    {
        var result = await new PaperReadingService(db.Context)
            .GetAsync(db.BobPaperId, PaperFlowTestDatabase.AliceId);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Reading_CannotUpdateAnotherUsersProgress()
    {
        var input = new PaperReadingDto
        {
            PaperId = db.BobPaperId,
            Status = "Reading",
            TotalPages = 50,
            CurrentPage = 10
        };
        bool changed = await new PaperReadingService(db.Context)
            .UpdateAsync(input, PaperFlowTestDatabase.AliceId);
        Assert.That(changed, Is.False);
        var unchanged = await db.Context.Papers.AsNoTracking()
            .SingleAsync(p => p.Id == db.BobPaperId);
        Assert.That(unchanged.CurrentPage, Is.Zero);
    }

    [Test]
    public async Task Reading_OwnerCanUpdateOwnProgress()
    {
        var input = new PaperReadingDto
        {
            PaperId = db.AlicePaperId,
            Status = "Reading",
            TotalPages = 100,
            CurrentPage = 35
        };
        bool changed = await new PaperReadingService(db.Context)
            .UpdateAsync(input, PaperFlowTestDatabase.AliceId);
        Assert.That(changed, Is.True);
        Assert.That((await db.Context.Papers.FindAsync(db.AlicePaperId))!.CurrentPage,
            Is.EqualTo(35));
    }

    [Test]
    public async Task Delete_CannotDeleteAnotherUsersPaper()
    {
        bool deleted = await new PaperDeleteService(db.Context)
            .DeleteAsync(db.BobPaperId, PaperFlowTestDatabase.AliceId);
        Assert.That(deleted, Is.False);
        Assert.That(await db.Context.Papers.AnyAsync(p => p.Id == db.BobPaperId), Is.True);
    }

    [Test]
    public async Task Delete_OwnerCanDeleteOwnPaper()
    {
        bool deleted = await new PaperDeleteService(db.Context)
            .DeleteAsync(db.AlicePaperId, PaperFlowTestDatabase.AliceId);
        Assert.That(deleted, Is.True);
        Assert.That(await db.Context.Papers.AnyAsync(p => p.Id == db.AlicePaperId), Is.False);
        Assert.That(await db.Context.Papers.AnyAsync(p => p.Id == db.BobPaperId), Is.True);
    }

    [Test]
    public async Task Count_OnlyCountsAuthenticatedUsersPapers()
    {
        var service = new PaperDeleteService(db.Context);
        var aliceCount = await service.CountAsync(PaperFlowTestDatabase.AliceId);
        var bobCount = await service.CountAsync(PaperFlowTestDatabase.BobId);
        Assert.Multiple(() =>
        {
            Assert.That(aliceCount, Is.EqualTo(1));
            Assert.That(bobCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Delete_RejectsMissingAuthenticatedUserId()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await new PaperDeleteService(db.Context).DeleteAsync(db.BobPaperId, ""));
    }
}

}

