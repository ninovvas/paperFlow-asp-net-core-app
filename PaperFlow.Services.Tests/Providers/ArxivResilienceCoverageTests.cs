namespace PaperFlow.Services.Tests.Providers
{
    using System.Net;
    using System.Net.Http.Headers;
    using System.Reflection;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging.Abstractions;
    using NUnit.Framework;
    using PaperFlow.Services.Http;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
[NonParallelizable] 
public sealed class ArxivResilienceCoverageTests
{
    [Test]
    public async Task Get_SuccessfulArxivFeed_IsCachedAndReused()
    {
        const string feed = "<feed xmlns=\"http://www.w3.org/2005/Atom\"/>";
        var downstream = new StubHttpHandler(HttpStatusCode.OK, feed);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(new ArxivResilienceHandler(
            cache, NullLogger<ArxivResilienceHandler>.Instance)
        {
            InnerHandler = downstream
        });

        // A unique URL keeps this test isolated from other test caches.
        string url = $"https://export.arxiv.org/api/query?search_query=all:test-{Guid.NewGuid():N}";
        using var first = await client.GetAsync(url);
        string firstBody = await first.Content.ReadAsStringAsync();
        using var second = await client.GetAsync(url);
        string secondBody = await second.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(firstBody, Is.EqualTo(feed));
            Assert.That(secondBody, Is.EqualTo(feed));
            Assert.That(downstream.CallCount, Is.EqualTo(1), "The second GET must be served from cache.");
        });
    }

    [Test]
    public void RetryDelay_ClampsSmallAndLargeRetryAfterValues()
    {
        MethodInfo? method = typeof(ArxivResilienceHandler).GetMethod(
            "RetryDelay", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Production method RetryDelay was renamed.");

        var tooShort = new RetryConditionHeaderValue(TimeSpan.Zero);
        var tooLong = new RetryConditionHeaderValue(TimeSpan.FromHours(2));
        var normal = new RetryConditionHeaderValue(TimeSpan.FromSeconds(15));

        var shortDelay = (TimeSpan)method!.Invoke(null, new object?[] { tooShort })!;
        var longDelay = (TimeSpan)method.Invoke(null, new object?[] { tooLong })!;
        var normalDelay = (TimeSpan)method.Invoke(null, new object?[] { normal })!;
        var defaultDelay = (TimeSpan)method.Invoke(null, new object?[] { null })!;

        Assert.Multiple(() =>
        {
            Assert.That(shortDelay, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(longDelay, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(normalDelay, Is.EqualTo(TimeSpan.FromSeconds(15)));
            Assert.That(defaultDelay, Is.EqualTo(TimeSpan.FromMinutes(5)));
        });
    }
}
}

