namespace PaperFlow.Services.Tests.Papers
{
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Common.Enums;
    using PaperFlow.Data.Models;
    using PaperFlow.Services.Interfaces.Providers;
    using PaperFlow.Services.Models.Search;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class PaperImportServiceTests
{
    private PaperFlowTestDatabase db = null!;
    private FakeArxivLookup lookup = null!;
    private PaperFlow.Services.PaperImportService service = null!;

    [SetUp]
    public void SetUp()
    {
        db = new PaperFlowTestDatabase();
        lookup = new FakeArxivLookup();
        service = new PaperFlow.Services.PaperImportService(db.Context, lookup,
            null!, null!, null!);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public void Import_EmptyUserId_IsRejectedBeforeApiCall()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ImportFromArxivAsync("2401.01234", " "));
        Assert.That(lookup.Calls, Is.Zero);
    }

    [Test]
    public async Task Import_MissingArxivMetadata_ReturnsNotFound()
    {
        var result = await service.ImportFromArxivAsync("2401.01234", "alice");
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.AlreadyExists, Is.False);
            Assert.That(lookup.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Import_SameUserSameArxivId_ReturnsDuplicateWithoutApiCall()
    {
        await db.SeedTwoUsersAsync();
        var source = new ResearchSource { Name = "arXiv", IsEnabled = true };
        db.Context.ResearchSources.Add(source);
        db.Context.Papers.Add(new Paper
        {
            Title = "Already imported research paper",
            UserId = PaperFlowTestDatabase.AliceId,
            ResearchSource = source,
            ExternalId = "2401.01234",
            Status = ReadingStatus.NotStarted
        });
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var result = await service.ImportFromArxivAsync("2401.01234", PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(result.AlreadyExists, Is.True);
            Assert.That(lookup.Calls, Is.Zero);
        });
    }

    [Test]
    public async Task Import_DuplicateInOtherUsersLibrary_DoesNotCountAsOwnDuplicate()
    {
        await db.SeedTwoUsersAsync();
        var source = new ResearchSource { Name = "arXiv", IsEnabled = true };
        db.Context.ResearchSources.Add(source);
        db.Context.Papers.Add(new Paper
        {
            Title = "Research publication from another account",
            UserId = PaperFlowTestDatabase.BobId,
            ResearchSource = source,
            ExternalId = "2401.01234"
        });
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        // No external metadata is returned: proves the current user's duplicate guard
        // did not incorrectly treat another user's paper as a duplicate.
        var result = await service.ImportFromArxivAsync("2401.01234", PaperFlowTestDatabase.AliceId);
        Assert.Multiple(() =>
        {
            Assert.That(result.AlreadyExists, Is.False);
            Assert.That(lookup.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void Import_InvalidArxivId_IsRejectedBeforeApiCall()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.ImportFromArxivAsync("not-an-arxiv-id", "alice"));
        Assert.That(lookup.Calls, Is.Zero);
    }

    private sealed class FakeArxivLookup : IArxivPaperLookupService
    {
        public int Calls { get; private set; }
        public Task<PaperSearchResult?> GetByIdAsync(string arxivId,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<PaperSearchResult?>(null);
        }
    }
}
}

