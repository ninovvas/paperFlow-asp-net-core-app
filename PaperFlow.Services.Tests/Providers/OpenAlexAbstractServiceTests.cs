namespace PaperFlow.Services.Tests.Providers
{
    using System.Net;
    using Microsoft.Extensions.Caching.Memory;
    using NUnit.Framework;
    using PaperFlow.Services.Providers.OpenAlex;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public class OpenAlexAbstractServiceTests
{
    [Test]
    public async Task MatchingDoi_ReturnsAbstractAndUsesCache()
    {
        const string json = """
        {"results":[{"id":"https://openalex.org/W123","display_name":"Learning robots","doi":"https://doi.org/10.1234/abc","abstract_inverted_index":{"Robot":[0],"learning":[1],"works":[2]}}]}
        """;
        var handler = new StubHttpHandler(HttpStatusCode.OK, json);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") };
        var service = new OpenAlexAbstractService(http, cache);
        var first = await service.GetAbstractByDoiAsync("10.1234/abc");
        var second = await service.GetAbstractByDoiAsync("10.1234/abc");
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("Robot learning works"));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task NoMatches_ReturnsNullAndCachesNegativeResult()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """{"results":[]}""");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") };
        var service = new OpenAlexAbstractService(http, cache);
        Assert.That(await service.GetAbstractByDoiAsync("10.1234/missing"), Is.Null);
        Assert.That(await service.GetAbstractByDoiAsync("10.1234/missing"), Is.Null);
        Assert.That(handler.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Http404_ReturnsNull()
    {
        var handler = new StubHttpHandler(HttpStatusCode.NotFound, "{}");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") };
        Assert.That(await new OpenAlexAbstractService(http, cache)
            .GetAbstractByDoiAsync("10.1234/missing"), Is.Null);
    }

    [Test]
    public void InvalidDoi_IsRejectedBeforeHttpRequest()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "{}");
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") };
        Assert.ThrowsAsync<ArgumentException>(async () => await new OpenAlexAbstractService(http, cache)
            .GetAbstractByDoiAsync("not-doi"));
        Assert.That(handler.CallCount, Is.Zero);
    }
}
}
