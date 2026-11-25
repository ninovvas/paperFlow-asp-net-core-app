namespace PaperFlow.Services.Tests.Statistics
{
    using PaperFlow.Services.Statistics;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class StatisticsServiceTests
{
    private PaperFlowTestDatabase db = null!;
    private StatisticsService service = null!;

    [SetUp]
    public async Task SetUp()
    {
        db = new PaperFlowTestDatabase();
        await db.SeedTwoUsersAsync();
        service = new StatisticsService(db.Context);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public void GetDashboard_EmptyUserIdIsRejected()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await service.GetDashboardAsync(""));
    }

    [Test]
    public async Task GetDashboard_UnknownUserReceivesZeroPapers()
    {
        var result = await service.GetDashboardAsync("unknown-user");
        Assert.That(result.TotalPapers, Is.Zero);
    }

    [Test]
    public async Task GetDashboard_OnlyCountsCurrentUsersPapers()
    {
        var alice = await service.GetDashboardAsync(PaperFlowTestDatabase.AliceId);
        var bob = await service.GetDashboardAsync(PaperFlowTestDatabase.BobId);
        Assert.That(alice.TotalPapers, Is.EqualTo(1));
        Assert.That(bob.TotalPapers, Is.EqualTo(1));
        Assert.That(alice.RecentPapers.Select(x => x.Id), Does.Not.Contain(db.BobPaperId));
        Assert.That(bob.RecentPapers.Select(x => x.Id), Does.Not.Contain(db.AlicePaperId));
    }
}
}

