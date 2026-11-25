namespace PaperFlow.Services.Tests.Papers
{
    using PaperFlow.Services.Papers;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class PaperDetailsServiceTests
{
    private PaperFlowTestDatabase db = null!;
    private PaperDetailsService service = null!;

    [SetUp]
    public async Task SetUp()
    {
        db = new PaperFlowTestDatabase();
        await db.SeedTwoUsersAsync();
        service = new PaperDetailsService(db.Context);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public async Task GetById_NonpositiveIdReturnsNull()
    {
        Assert.That(await service.GetByIdAsync(0, PaperFlowTestDatabase.AliceId), Is.Null);
    }

    [Test]
    public async Task GetById_UnknownIdReturnsNull()
    {
        Assert.That(await service.GetByIdAsync(int.MaxValue, PaperFlowTestDatabase.AliceId), Is.Null);
    }

    [Test]
    public void GetById_EmptyUserIdThrows()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.GetByIdAsync(db.AlicePaperId, ""));
    }

    [Test]
    public async Task GetById_OwnerReceivesReadingProgress()
    {
        var details = await service.GetByIdAsync(db.AlicePaperId, PaperFlowTestDatabase.AliceId);
        Assert.That(details, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(details!.CurrentPage, Is.EqualTo(20));
            Assert.That(details.TotalPages, Is.EqualTo(100));
            Assert.That(details.ReadingProgressPercent, Is.EqualTo(20));
        });
    }
}


}
