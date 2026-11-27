namespace PaperFlow.Services.Tests.Papers
{
    using NUnit.Framework;
    using PaperFlow.Services.Models.Papers;
    using PaperFlow.Services.Papers;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public class PaperEditValidationTests
{
    private PaperFlowTestDatabase db = null!;
    private PaperEditService service = null!;

    [SetUp]
    public void SetUp()
    {
        db = new PaperFlowTestDatabase();
        service = new PaperEditService(db.Context);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public void Update_NullDto_IsRejectedBeforeTransaction() =>
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.UpdateAsync(null!, PaperFlowTestDatabase.AliceId));

    [TestCase("")]
    [TestCase("  ")]
    public void Update_EmptyUserId_IsRejectedBeforeTransaction(string userId) =>
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.UpdateAsync(new PaperEditDto { Title = "Valid paper title" }, userId));

    [TestCase("")]
    [TestCase("a")]
    public async Task Update_InvalidTitle_ReturnsValidationFailed(string title)
    {
        var result = await service.UpdateAsync(new PaperEditDto { Id = 1, Title = title },
            PaperFlowTestDatabase.AliceId);
        Assert.That(result.Status, Is.EqualTo(PaperEditStatus.ValidationFailed));
    }

    [Test]
    public async Task Update_InvalidUrl_ReturnsValidationFailed()
    {
        var result = await service.UpdateAsync(new PaperEditDto
        {
            Id = 1,
            Title = "Valid research title",
            PublicationUrl = "javascript:alert(1)"
        }, PaperFlowTestDatabase.AliceId);
        Assert.That(result.Status, Is.EqualTo(PaperEditStatus.ValidationFailed));
    }

    [Test]
    public async Task GetAsync_OnlyReturnsOwnedPaper()
    {
        await db.SeedTwoUsersAsync();
        var own = await service.GetAsync(db.AlicePaperId, PaperFlowTestDatabase.AliceId);
        var foreign = await service.GetAsync(db.BobPaperId, PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(own, Is.Not.Null);
            Assert.That(foreign, Is.Null);
        });
    }

    [Test]
    public async Task GetAsync_InvalidId_ReturnsNull()
    {
        Assert.That(await service.GetAsync(0, PaperFlowTestDatabase.AliceId), Is.Null);
    }
}
}

