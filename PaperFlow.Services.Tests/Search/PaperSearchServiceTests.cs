namespace PaperFlow.Services.Tests.Search
{
    using Microsoft.Extensions.Logging.Abstractions;
    using PaperFlow.Services.Admin;
    using PaperFlow.Services.Interfaces.Providers;
    using PaperFlow.Services.Models.Search;
    using PaperFlow.Services.Search;
    using PaperFlow.Services.Tests.Infrastructure;


    [TestFixture]
    public sealed class PaperSearchServiceTests
    {
        private PaperFlowTestDatabase db = null!;
        private ResearchSourceManagementService sources = null!;
        private FakeProvider arxiv = null!;
        private FakeProvider crossref = null!;
        private PaperSearchService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = new PaperFlowTestDatabase();
            sources = new ResearchSourceManagementService(db.Context);
            arxiv = new FakeProvider("arxiv", "arXiv");
            crossref = new FakeProvider("crossref", "Crossref");
            service = new PaperSearchService(
                [arxiv, crossref],
                NullLogger<PaperSearchService>.Instance,
                new FakeAbstractService(),
                sources);
        }

        [TearDown]
        public async Task TearDown() => await db.DisposeAsync();

        [Test]
        public async Task Search_SelectedEnabledProvider_InvokesOnlySelectedProvider()
        {
            arxiv.Results.Add(Paper("arxiv", "1234.56789", "Mobile robot navigation"));
            var result = await service.SearchAsync(new PaperSearchCriteria { Keywords = "robot" }, ["arxiv"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.TotalResults, Is.EqualTo(1));
                Assert.That(arxiv.Calls, Is.EqualTo(1));
                Assert.That(crossref.Calls, Is.Zero);
            });
        }

        [Test]
        public async Task Search_DisabledProvider_IsRejectedBeforeExternalCall()
        {
            await sources.SetEnabledAsync("crossref", false);
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.SearchAsync(new PaperSearchCriteria { Keywords = "robot" }, ["crossref"]));
            Assert.That(crossref.Calls, Is.Zero);
        }

        [Test]
        public async Task Search_All_UsesOnlyEnabledProviders()
        {
            await sources.SetEnabledAsync("crossref", false);
            arxiv.Results.Add(Paper("arxiv", "1234.56789", "Navigation research"));
            var result = await service.SearchAsync(new PaperSearchCriteria(), ["all"]);
            Assert.Multiple(() =>
            {
                Assert.That(arxiv.Calls, Is.EqualTo(1));
                Assert.That(crossref.Calls, Is.Zero);
                Assert.That(result.TotalResults, Is.EqualTo(1));
            });
        }

        [Test]
        public void Search_UnknownProvider_IsRejected()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.SearchAsync(new PaperSearchCriteria(), ["forged-provider"]));
        }

        [Test]
        public void Search_EmptySelection_IsRejected()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.SearchAsync(new PaperSearchCriteria(), Array.Empty<string>()));
        }

        [Test]
        public async Task Search_ProviderFailure_DoesNotDiscardOtherProviderResults()
        {
            arxiv.Results.Add(Paper("arxiv", "1234.56789", "Research paper"));
            crossref.Failure = new HttpRequestException("Rate limited", null,
                System.Net.HttpStatusCode.TooManyRequests);
            var result = await service.SearchAsync(new PaperSearchCriteria(), ["arxiv", "crossref"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.TotalResults, Is.EqualTo(1));
                Assert.That(result.SuccessfulSources, Does.Contain("arXiv"));
                Assert.That(result.FailedSources.Single(), Does.Contain("rate limit"));
            });
        }

        [Test]
        public async Task Search_SameDoiAcrossProviders_ReturnsOnePaper()
        {
            var first = Paper("arxiv", "a1", "Robotics paper"); first.Doi = "10.1234/ROBOT";
            var second = Paper("crossref", "b1", "Robotics paper"); second.Doi = "https://doi.org/10.1234/robot";
            arxiv.Results.Add(first);
            crossref.Results.Add(second);
            var result = await service.SearchAsync(new PaperSearchCriteria(), ["arxiv", "crossref"]);
            Assert.That(result.TotalResults, Is.EqualTo(1));
        }

        [Test]
        public async Task Search_DuplicateSelectedSource_IsOnlyInvokedOnce()
        {
            await service.SearchAsync(new PaperSearchCriteria(), ["arxiv", "ARXIV", "arxiv"]);
            Assert.That(arxiv.Calls, Is.EqualTo(1));
        }

        private static PaperSearchResult Paper(string source, string externalId, string title) => new()
        {
            SourceName = source,
            ExternalId = externalId,
            Title = title
        };

        private sealed class FakeAbstractService : IOpenAlexAbstractService
        {
            public Task<string?> GetAbstractByDoiAsync(string doi, CancellationToken cancellationToken = default)
                => Task.FromResult<string?>(null);
        }

        private sealed class FakeProvider(string providerId, string providerName) : IResearchPaperProvider
        {
            public string ProviderId => providerId;
            public string ProviderName => providerName;
            public int Calls { get; private set; }
            public List<PaperSearchResult> Results { get; } = [];
            public Exception? Failure { get; set; }

            public Task<IReadOnlyList<PaperSearchResult>> SearchAsync(
                string query, int maxResults = 10, CancellationToken cancellationToken = default) =>
                SearchAsync(new PaperSearchCriteria { Keywords = query, MaxResults = maxResults }, cancellationToken);

            public Task<IReadOnlyList<PaperSearchResult>> SearchAsync(
                PaperSearchCriteria criteria, CancellationToken cancellationToken = default)
            {
                Calls++;
                if (Failure != null) throw Failure;
                return Task.FromResult<IReadOnlyList<PaperSearchResult>>(Results.ToList());
            }
        }
    }
}

