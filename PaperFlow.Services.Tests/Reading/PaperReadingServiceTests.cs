namespace PaperFlow.Services.Tests.Reading
{
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Services.Models.Reading;
    using PaperFlow.Services.Reading;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class PaperReadingServiceTests
{
    private PaperFlowTestDatabase db = null!;
    private PaperReadingService service = null!;

    [SetUp]
    public async Task SetUp()
    {
        db = new PaperFlowTestDatabase();
        await db.SeedTwoUsersAsync();
        service = new PaperReadingService(db.Context);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [TestCase("Invalid")]
    [TestCase("completed")]
    public void Update_RejectsInvalidStatus(string status)
    {
        var dto = new PaperReadingDto { PaperId = db.AlicePaperId, Status = status };
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.UpdateAsync(dto, PaperFlowTestDatabase.AliceId));
    }

    [TestCase(0)]
    [TestCase(6)]
    public void Update_RejectsInvalidRating(int rating)
    {
        var dto = new PaperReadingDto { PaperId = db.AlicePaperId, Rating = rating };
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.UpdateAsync(dto, PaperFlowTestDatabase.AliceId));
    }

    [Test]
    public void Update_RejectsCurrentPageBeyondTotalPages()
    {
        var dto = new PaperReadingDto
        {
            PaperId = db.AlicePaperId,
            Status = "Reading",
            TotalPages = 10,
            CurrentPage = 11
        };
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.UpdateAsync(dto, PaperFlowTestDatabase.AliceId));
    }

    [Test]
    public void Update_RejectsMissingUserId()
    {
        var dto = new PaperReadingDto { PaperId = db.AlicePaperId };
        Assert.ThrowsAsync<ArgumentException>(async () => await service.UpdateAsync(dto, ""));
    }

    [Test]
    public async Task Update_CompletedPaperSetsProgressAndCompletionDate()
    {
        var dto = new PaperReadingDto
        {
            PaperId = db.AlicePaperId,
            Status = "Completed",
            TotalPages = 100,
            CurrentPage = 30,
            Rating = 5
        };
        var result = await service.UpdateAsync(dto, PaperFlowTestDatabase.AliceId);
        Assert.That(result, Is.True);
        var paper = await db.Context.Papers.SingleAsync(p => p.Id == db.AlicePaperId);
        Assert.Multiple(() =>
        {
            Assert.That(paper.Status.ToString(), Is.EqualTo("Completed"));
            Assert.That(paper.CurrentPage, Is.EqualTo(100));
            Assert.That(paper.CompletedOn, Is.Not.Null);
            Assert.That(paper.Rating, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task Update_UnknownPaperReturnsFalse()
    {
        var dto = new PaperReadingDto { PaperId = int.MaxValue, Status = "NotStarted" };
        Assert.That(await service.UpdateAsync(dto, PaperFlowTestDatabase.AliceId), Is.False);
    }
}
}

