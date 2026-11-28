namespace PaperFlow.Services.Tests.Extensions
{


    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using PaperFlow.Services.Extensions;
    using PaperFlow.Services.Interfaces.Papers;
    using PaperFlow.Services.Interfaces.Providers;
    using PaperFlow.Services.Interfaces.Reading;
    using PaperFlow.Services.Interfaces.Search;
    using PaperFlow.Services.Interfaces.Statistics;
    using PaperFlow.Services.Papers;
    using PaperFlow.Services.Providers.Arxiv;
    using PaperFlow.Services.Providers.Crossref;
    using PaperFlow.Services.Providers.OpenAlex;
    using PaperFlow.Services.Reading;
    using PaperFlow.Services.Search;
    using PaperFlow.Services.Statistics;

    [TestFixture]
    public sealed class ServiceCollectionExtensionsTests
    {
        private ServiceCollection services = null!;
        private ServiceProvider provider = null!;

        [SetUp]
        public void SetUp()
        {
            services = new ServiceCollection();
            services.AddPaperFlowServices();
            provider = services.BuildServiceProvider(validateScopes: true);
        }

        [TearDown]
        public void TearDown()
        {
            provider.Dispose();
        }

        [Test]
        public void AddPaperFlowServices_ReturnsTheSameCollection()
        {
            var collection = new ServiceCollection();
            var returned = collection.AddPaperFlowServices();
            Assert.That(returned, Is.SameAs(collection));
        }

        [TestCase(typeof(IPaperSearchService), typeof(PaperSearchService))]
        [TestCase(typeof(IPaperImportService), typeof(PaperImportService))]
        [TestCase(typeof(IPaperDetailsService), typeof(PaperDetailsService))]
        [TestCase(typeof(IPaperReadingService), typeof(PaperReadingService))]
        [TestCase(typeof(IPaperEditService), typeof(PaperEditService))]
        [TestCase(typeof(IPaperDeleteService), typeof(PaperDeleteService))]
        [TestCase(typeof(IPaperSavedStatusService), typeof(PaperSavedStatusService))]
        [TestCase(typeof(IStatisticsService), typeof(StatisticsService))]
        public void ApplicationServices_AreRegisteredExactlyOnceAsScoped(
            Type serviceType, Type implementationType)
        {
            var matches = services.Where(x => x.ServiceType == serviceType).ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(matches, Has.Length.EqualTo(1));
                Assert.That(matches[0].Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(matches[0].ImplementationType, Is.EqualTo(implementationType));
            });
        }

        [Test]
        public void ArxivInfrastructure_HasExpectedLifetimes()
        {
            var coordinator = services.Single(x => x.ServiceType == typeof(ArxivRequestCoordinator));
            var handler = services.Single(x => x.ServiceType == typeof(ArxivRateLimitHandler));
            Assert.Multiple(() =>
            {
                Assert.That(coordinator.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
                Assert.That(handler.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
            });
        }

        [Test]
        public void ProviderDiscovery_ResolvesAllThreeImplementations()
        {
            using var scope = provider.CreateScope();
            var found = scope.ServiceProvider.GetServices<IResearchPaperProvider>().ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(found, Has.Length.EqualTo(3));
                Assert.That(found.Select(x => x.ProviderId), Is.EquivalentTo(new[] { "arxiv", "crossref", "openalex" }));
                Assert.That(found.Count(x => x is ArxivSearchService), Is.EqualTo(1));
                Assert.That(found.Count(x => x is CrossrefSearchService), Is.EqualTo(1));
                Assert.That(found.Count(x => x is OpenAlexSearchService), Is.EqualTo(1));
            });
        }

        [Test]
        public void ProviderDiscovery_UsesRegisteredTypedImplementations()
        {
            using var scope = provider.CreateScope();
            var sp = scope.ServiceProvider;
            var discovered = sp.GetServices<IResearchPaperProvider>().ToArray();

            // Typed HttpClients are transient, so a subsequent resolution creates a new instance.
            Assert.Multiple(() =>
            {
                Assert.That(discovered.Single(x => x.ProviderId == "arxiv"), Is.TypeOf<ArxivSearchService>());
                Assert.That(discovered.Single(x => x.ProviderId == "crossref"), Is.TypeOf<CrossrefSearchService>());
                Assert.That(discovered.Single(x => x.ProviderId == "openalex"), Is.TypeOf<OpenAlexSearchService>());
                Assert.That(sp.GetRequiredService<ArxivSearchService>(), Is.TypeOf<ArxivSearchService>());
                Assert.That(sp.GetRequiredService<CrossrefSearchService>(), Is.TypeOf<CrossrefSearchService>());
                Assert.That(sp.GetRequiredService<OpenAlexSearchService>(), Is.TypeOf<OpenAlexSearchService>());
            });
        }

        [Test]
        public void TypedLookupServices_ResolveWithoutCallingExternalApis()
        {
            using var scope = provider.CreateScope();
            var sp = scope.ServiceProvider;
            Assert.Multiple(() =>
            {
                Assert.That(sp.GetRequiredService<IArxivPaperLookupService>(), Is.TypeOf<ArxivPaperLookupService>());
                Assert.That(sp.GetRequiredService<ICrossrefPaperLookupService>(), Is.TypeOf<CrossrefPaperLookupService>());
                Assert.That(sp.GetRequiredService<IOpenAlexWorkLookupService>(), Is.TypeOf<OpenAlexWorkLookupService>());
                Assert.That(sp.GetRequiredService<IOpenAlexAbstractService>(), Is.TypeOf<OpenAlexAbstractService>());
                Assert.That(sp.GetRequiredService<IMemoryCache>(), Is.Not.Null);
            });
        }

        [TestCase("CrossrefSearchService")]
        [TestCase("ICrossrefPaperLookupService")]
        public void CrossrefClients_UseCrossrefEndpointAndExpectedSettings(string clientName)
        {
            using var client = GetClient(clientName);
            Assert.Multiple(() =>
            {
                Assert.That(client.BaseAddress, Is.EqualTo(new Uri("https://api.crossref.org/")));
                Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
                Assert.That(client.DefaultRequestHeaders.Accept.Any(x => x.MediaType == "application/json"), Is.True);
                Assert.That(client.DefaultRequestHeaders.UserAgent.ToString(), Does.Contain("PaperFlow/1.0"));
            });
        }

        [TestCase("OpenAlexSearchService")]
        [TestCase("IOpenAlexWorkLookupService")]
        [TestCase("IOpenAlexAbstractService")]
        public void OpenAlexClients_UseOpenAlexEndpointAndExpectedSettings(string clientName)
        {
            using var client = GetClient(clientName);
            Assert.Multiple(() =>
            {
                Assert.That(client.BaseAddress, Is.EqualTo(new Uri("https://api.openalex.org/")));
                Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(20)));
                Assert.That(client.DefaultRequestHeaders.Accept.Any(x => x.MediaType == "application/json"), Is.True);
                Assert.That(client.DefaultRequestHeaders.UserAgent.ToString(), Does.Contain("PaperFlow/1.0"));
            });
        }

        [TestCase("ArxivSearchService")]
        [TestCase("IArxivPaperLookupService")]
        public void ArxivClients_UseExpectedTimeoutAndUserAgent(string clientName)
        {
            using var client = GetClient(clientName);
            Assert.Multiple(() =>
            {
                Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(120)));
                Assert.That(client.DefaultRequestHeaders.UserAgent.ToString(), Does.Contain("PaperFlow/1.0"));
                // arXiv's URL is constructed by the provider itself, not as HttpClient.BaseAddress.
                Assert.That(client.BaseAddress, Is.Null);
            });
        }

        [Test]
        public void Providers_HaveIndependentNamedHttpClientConfigurations()
        {
            using var crossref = GetClient(nameof(CrossrefSearchService));
            using var openAlex = GetClient(nameof(OpenAlexSearchService));
            using var arxiv = GetClient(nameof(ArxivSearchService));
            Assert.Multiple(() =>
            {
                Assert.That(crossref.BaseAddress, Is.Not.EqualTo(openAlex.BaseAddress));
                Assert.That(crossref.Timeout, Is.Not.EqualTo(openAlex.Timeout));
                Assert.That(arxiv.Timeout, Is.Not.EqualTo(crossref.Timeout));
            });
        }

        private HttpClient GetClient(string name) => provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(name);
    }
}

