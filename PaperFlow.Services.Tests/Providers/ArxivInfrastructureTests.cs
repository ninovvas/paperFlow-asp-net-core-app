namespace PaperFlow.Services.Tests.Providers
{
    using System.Net;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging.Abstractions;
    using NUnit.Framework;
    using PaperFlow.Services.Http;
    using PaperFlow.Services.Providers.Arxiv;
    using PaperFlow.Services.Tests.Infrastructure;


    [TestFixture]
    public class ArxivInfrastructureTests
    {
        [Test]
        public async Task Coordinator_FirstRequestInvokesDelegateAndReturnsResponse()
        {
            var coordinator = new ArxivRequestCoordinator();
            int calls = 0;
            using var response = await coordinator.SendAsync(_ =>
            {
                calls++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public async Task RateLimitHandler_FirstRequestIsForwarded()
        {
            var handler = new StubHttpHandler(HttpStatusCode.OK, "ok");
            using var http = new HttpClient(new ArxivRateLimitHandler(new ArxivRequestCoordinator())
            {
                InnerHandler = handler
            });
            using var response = await http.GetAsync("https://example.test/query");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ResilienceHandler_OtherHostsPassThroughWithoutCaching()
        {
            var handler = new StubHttpHandler(HttpStatusCode.OK, "ok");
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var http = new HttpClient(new ArxivResilienceHandler(
                cache, NullLogger<ArxivResilienceHandler>.Instance)
            {
                InnerHandler = handler
            });
            using var first = await http.GetAsync("https://example.test/query");
            using var second = await http.GetAsync("https://example.test/query");
            Assert.That(handler.CallCount, Is.EqualTo(2));
        }
    }
}

