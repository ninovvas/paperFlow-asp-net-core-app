namespace PaperFlow.Services.Tests.Papers
{
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Data.Models;
    using PaperFlow.Services.Interfaces.Providers;
    using PaperFlow.Services.Models.Search;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class PaperImportGuardCoverageTests
{
    private PaperFlowTestDatabase db = null!;
    private FakeCrossrefLookup crossref = null!;
    private FakeOpenAlexLookup openAlex = null!;
    private FakeAbstractLookup abstracts = null!;
    private PaperFlow.Services.PaperImportService service = null!;

    [SetUp]
    public void SetUp()
    {
        db = new PaperFlowTestDatabase();
        crossref = new FakeCrossrefLookup();
        openAlex = new FakeOpenAlexLookup();
        abstracts = new FakeAbstractLookup();
        service = new PaperFlow.Services.PaperImportService(
            db.Context, null!, crossref, openAlex, abstracts);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [TestCase("")]
    [TestCase("   ")]
    public void Crossref_EmptyUserId_IsRejectedBeforeLookup(string userId)
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ImportFromCrossrefAsync("10.1234/test", userId));
        Assert.That(crossref.Calls, Is.Zero);
    }

    [Test]
    public void Crossref_InvalidDoi_IsRejectedBeforeLookup()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ImportFromCrossrefAsync("invalid-doi", PaperFlowTestDatabase.AliceId));
        Assert.That(crossref.Calls, Is.Zero);
    }

    [Test]
    public async Task Crossref_LookupReturnsNull_DoesNotPersistPaper()
    {
        var result = await service.ImportFromCrossrefAsync("10.1234/missing", PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(crossref.Calls, Is.EqualTo(1));
            Assert.That(abstracts.Calls, Is.Zero);
        });
        Assert.That(await db.Context.Papers.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Crossref_ExistingDoiForSameUser_DoesNotCallLookup()
    {
        await db.SeedTwoUsersAsync();
        var saved = await db.Context.Papers.FindAsync(db.AlicePaperId);
        saved!.Doi = "10.1234/reused";
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var result = await service.ImportFromCrossrefAsync("https://doi.org/10.1234/REUSED", PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(result.AlreadyExists, Is.True);
            Assert.That(crossref.Calls, Is.Zero);
        });
    }

    [Test]
    public async Task Crossref_DuplicateDoiFromAnotherUser_DoesNotBlockLookup()
    {
        await db.SeedTwoUsersAsync();
        var saved = await db.Context.Papers.FindAsync(db.BobPaperId);
        saved!.Doi = "10.1234/bob";
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var result = await service.ImportFromCrossrefAsync("10.1234/bob", PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(result.AlreadyExists, Is.False);
            Assert.That(crossref.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Crossref_InvalidReturnedTitle_IsRejectedBeforeTransaction()
    {
        crossref.Result = new PaperSearchResult
        {
            Doi = "10.1234/valid",
            Title = "Bad",
            Abstract = "Already available"
        };
        var result = await service.ImportFromCrossrefAsync("10.1234/valid", PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(abstracts.Calls, Is.Zero);
        });
    }

    [Test]
    public async Task Crossref_MismatchedReturnedDoi_IsRejectedBeforeTransaction()
    {
        crossref.Result = new PaperSearchResult
        {
            Doi = "10.1234/wrong",
            Title = "A valid research publication",
            Abstract = "Already available"
        };
        var result = await service.ImportFromCrossrefAsync("10.1234/requested", PaperFlowTestDatabase.AliceId);
        Assert.That(result.Success, Is.False);
        Assert.That(await db.Context.Papers.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Crossref_AbstractEnrichmentFailure_DoesNotHideValidationError()
    {
        crossref.Result = new PaperSearchResult { Doi = "10.1234/valid", Title = "Bad" };
        abstracts.Error = new HttpRequestException("OpenAlex unavailable");
        var result = await service.ImportFromCrossrefAsync("10.1234/valid", PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(abstracts.Calls, Is.EqualTo(1));
        });
    }

    [TestCase("")]
    [TestCase("  ")]
    public void OpenAlex_EmptyUserId_IsRejectedBeforeLookup(string userId)
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ImportFromOpenAlexAsync("W123456", userId));
        Assert.That(openAlex.Calls, Is.Zero);
    }

    [Test]
    public void OpenAlex_InvalidId_IsRejectedBeforeLookup()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ImportFromOpenAlexAsync("not-a-work", PaperFlowTestDatabase.AliceId));
        Assert.That(openAlex.Calls, Is.Zero);
    }

    [Test]
    public async Task OpenAlex_LookupReturnsNull_DoesNotPersistPaper()
    {
        var result = await service.ImportFromOpenAlexAsync("W123456", PaperFlowTestDatabase.AliceId);
        Assert.That(result.Success, Is.False);
        Assert.That(openAlex.Calls, Is.EqualTo(1));
        Assert.That(await db.Context.Papers.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task OpenAlex_ExistingWorkIdForSameUser_DoesNotCallLookup()
    {
        await db.SeedTwoUsersAsync();
        var source = new ResearchSource { Name = "OpenAlex", IsEnabled = true };
        var saved = await db.Context.Papers.FindAsync(db.AlicePaperId);
        saved!.ResearchSource = source;
        saved.ExternalId = "W123456";
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var result = await service.ImportFromOpenAlexAsync("https://openalex.org/W123456", PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(result.AlreadyExists, Is.True);
            Assert.That(openAlex.Calls, Is.Zero);
        });
    }

    [Test]
    public async Task OpenAlex_MismatchedReturnedId_IsRejectedBeforeTransaction()
    {
        openAlex.Result = new PaperSearchResult { ExternalId = "W999999", Title = "A valid research paper" };
        var result = await service.ImportFromOpenAlexAsync("W123456", PaperFlowTestDatabase.AliceId);
        Assert.That(result.Success, Is.False);
        Assert.That(await db.Context.Papers.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task OpenAlex_InvalidReturnedTitle_IsRejectedBeforeTransaction()
    {
        openAlex.Result = new PaperSearchResult { ExternalId = "W123456", Title = "Bad" };
        var result = await service.ImportFromOpenAlexAsync("W123456", PaperFlowTestDatabase.AliceId);
        Assert.That(result.Success, Is.False);
        Assert.That(await db.Context.Papers.CountAsync(), Is.Zero);
    }

    private sealed class FakeCrossrefLookup : ICrossrefPaperLookupService
    {
        public int Calls { get; private set; }
        public PaperSearchResult? Result { get; set; }
        public Task<PaperSearchResult?> GetByDoiAsync(string doi, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeOpenAlexLookup : IOpenAlexWorkLookupService
    {
        public int Calls { get; private set; }
        public PaperSearchResult? Result { get; set; }
        public Task<PaperSearchResult?> GetByIdAsync(string workId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAbstractLookup : IOpenAlexAbstractService
    {
        public int Calls { get; private set; }
        public Exception? Error { get; set; }
        public Task<string?> GetAbstractByDoiAsync(string doi, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Error != null) return Task.FromException<string?>(Error);
            return Task.FromResult<string?>(null);
        }
    }
}
}
