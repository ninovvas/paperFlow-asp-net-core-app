namespace PaperFlow.Services.Tests.Search
{
    using NUnit.Framework;
    using PaperFlow.Data.Models;
    using PaperFlow.Services.Search;
    using PaperFlow.Services.Models.Search;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public class PaperSavedStatusServiceTests
{
    private PaperFlowTestDatabase db = null!;
    private PaperSavedStatusService service = null!;

    [SetUp]
    public void SetUp()
    {
        db = new PaperFlowTestDatabase();
        service = new PaperSavedStatusService(db.Context);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Arxiv_RejectsMissingUserId(string? userId) =>
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.GetSavedArxivIdsAsync(userId!, Array.Empty<string>()));

    [Test]
    public void Arxiv_RejectsNullIds() =>
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.GetSavedArxivIdsAsync("alice", null!));

    [Test]
    public async Task Arxiv_EmptyInput_ReturnsEmptySet()
    {
        var result = await service.GetSavedArxivIdsAsync("alice", ["", "  "]);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Arxiv_UnknownSource_ReturnsEmptySet()
    {
        var result = await service.GetSavedArxivIdsAsync("alice", ["2401.01234"]);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Arxiv_OnlyMatchesOwnerAndSource()
    {
        await db.SeedTwoUsersAsync();
        var source = new ResearchSource { Name = "arXiv" };
        db.Context.ResearchSources.Add(source);
        await db.Context.SaveChangesAsync();
        db.Context.Papers.AddRange(
            new Paper
            {
                Title = "Alice paper",
                UserId = PaperFlowTestDatabase.AliceId,
                ResearchSourceId = source.Id,
                ExternalId = "2401.01234"
            },
            new Paper
            {
                Title = "Bob paper",
                UserId = PaperFlowTestDatabase.BobId,
                ResearchSourceId = source.Id,
                ExternalId = "2401.99999"
            });
        await db.Context.SaveChangesAsync();

        var ids = await service.GetSavedArxivIdsAsync(PaperFlowTestDatabase.AliceId,
            ["2401.01234", "2401.99999", "2401.01234"]);
        Assert.That(ids, Is.EquivalentTo(new[] { "2401.01234" }));
    }

    [Test]
    public void Crossref_RejectsNullDois() =>
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.GetSavedCrossrefIdsAsync("alice", null!));

    [Test]
    public async Task Crossref_EmptyInput_ReturnsEmpty()
    {
        Assert.That(await service.GetSavedCrossrefIdsAsync("alice", [" ", ""]), Is.Empty);
    }

    [Test]
    public async Task Crossref_OnlyOwnDoisAreReturned()
    {
        await db.SeedTwoUsersAsync();
        db.Context.Papers.AddRange(
            new Paper { Title = "Alice DOI", UserId = PaperFlowTestDatabase.AliceId, Doi = "10.1234/alice" },
            new Paper { Title = "Bob DOI", UserId = PaperFlowTestDatabase.BobId, Doi = "10.1234/bob" });
        await db.Context.SaveChangesAsync();

        var result = await service.GetSavedCrossrefIdsAsync(PaperFlowTestDatabase.AliceId,
            ["10.1234/alice", "10.1234/bob"]);
        Assert.That(result, Is.EquivalentTo(new[] { "10.1234/alice" }));
    }

    [Test]
    public async Task OpenAlex_EmptyResults_ReturnEmpty()
    {
        Assert.That(await service.GetSavedOpenAlexIdsAsync("alice", Array.Empty<PaperSearchResult>()), Is.Empty);
    }

    [Test]
    public async Task OpenAlex_RecognizesSavedDoiFromAnotherSource()
    {
        await db.SeedTwoUsersAsync();
        db.Context.Papers.Add(new Paper
        {
            Title = "Existing DOI",
            UserId = PaperFlowTestDatabase.AliceId,
            Doi = "10.1234/matched"
        });
        await db.Context.SaveChangesAsync();
        PaperSearchResult[] results =
        [
            new() { SourceName = "OpenAlex", ExternalId = "W123", Doi = "10.1234/matched" },
            new() { SourceName = "OpenAlex", ExternalId = "W456", Doi = "10.1234/different" },
            new() { SourceName = "Crossref", ExternalId = "10.1234/matched", Doi = "10.1234/matched" }
        ];
        var saved = await service.GetSavedOpenAlexIdsAsync(PaperFlowTestDatabase.AliceId, results);
        Assert.That(saved, Is.EquivalentTo(new[] { "W123" }));
    }
}

}

