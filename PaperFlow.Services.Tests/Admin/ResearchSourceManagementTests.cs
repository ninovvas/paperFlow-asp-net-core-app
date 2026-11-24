namespace PaperFlow.Services.Tests.Admin
{
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Services.Admin;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class ResearchSourceManagementTests
{
    private PaperFlowTestDatabase db = null!;
    private ResearchSourceManagementService service = null!;

    [SetUp]
    public void SetUp()
    {
        db = new PaperFlowTestDatabase();
        service = new ResearchSourceManagementService(db.Context);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public async Task GetSources_SeedsThreeKnownSources()
    {
        var sources = await service.GetSourcesAsync();
        Assert.That(sources.Select(x => x.ProviderId),
            Is.EquivalentTo(new[] { "arxiv", "crossref", "openalex" }));
        Assert.That(await db.Context.ResearchSources.CountAsync(), Is.EqualTo(3));
    }

    [Test]
    public async Task GetSources_WhenCalledTwice_DoesNotDuplicateSources()
    {
        await service.GetSourcesAsync();
        await service.GetSourcesAsync();
        Assert.That(await db.Context.ResearchSources.CountAsync(), Is.EqualTo(3));
    }

    [Test]
    public async Task SetEnabled_UnknownProviderDoesNotChangeDatabase()
    {
        var result = await service.SetEnabledAsync("unknown-provider", false);
        Assert.That(result, Is.False);
        Assert.That(await db.Context.ResearchSources.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task SetEnabled_DisabledProviderIsOmittedFromEnabledList()
    {
        Assert.That(await service.SetEnabledAsync("crossref", false), Is.True);
        var enabled = await service.GetEnabledProviderIdsAsync();
        Assert.That(enabled, Does.Not.Contain("crossref"));
        Assert.That(enabled, Does.Contain("arxiv"));
        Assert.That(enabled, Does.Contain("openalex"));
    }

    [Test]
    public async Task SetEnabled_ProviderCanBeReenabled()
    {
        await service.SetEnabledAsync("crossref", false);
        await service.SetEnabledAsync("crossref", true);
        var enabled = await service.GetEnabledProviderIdsAsync();
        Assert.That(enabled, Does.Contain("crossref"));
    }

    [Test]
    public async Task GetSources_DoesNotResetDisabledProvider()
    {
        await service.SetEnabledAsync("openalex", false);
        var sources = await service.GetSourcesAsync();
        Assert.That(sources.Single(s => s.ProviderId == "openalex").IsEnabled, Is.False);
    }

    [Test]
    public async Task GetEnabledProviderIds_WithNoRowsPreservesDefaultAvailability()
    {
        var enabled = await service.GetEnabledProviderIdsAsync();
        Assert.That(enabled.Count, Is.EqualTo(3));
    }
}
}
