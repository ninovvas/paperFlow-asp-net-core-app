namespace PaperFlow.Services.Tests.Providers
{
    using System.Net;
    using NUnit.Framework;
    using PaperFlow.Services.Models.Search;
    using PaperFlow.Services.Providers.Arxiv;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public class ArxivHttpTests
{
    private static HttpClient Client(StubHttpHandler handler) => new(handler);

    [Test]
    public async Task Search_EmptyFeedReturnsNoPapers()
    {
        var service = new ArxivSearchService(Client(new StubHttpHandler(HttpStatusCode.OK,
            """<?xml version="1.0"?><feed xmlns="http://www.w3.org/2005/Atom"></feed>""")));
        var results = await service.SearchAsync(new PaperSearchCriteria { Keywords = "robot" });
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Search_HttpErrorIsPropagated()
    {
        var service = new ArxivSearchService(Client(new StubHttpHandler(HttpStatusCode.ServiceUnavailable, "")));
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.SearchAsync("robot"));
    }

    [Test]
    public async Task Lookup_EmptyFeedReturnsNull()
    {
        var service = new ArxivPaperLookupService(Client(new StubHttpHandler(HttpStatusCode.OK,
            """<?xml version="1.0"?><feed xmlns="http://www.w3.org/2005/Atom"></feed>""")));
        var result = await service.GetByIdAsync("2401.01234");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Lookup_RejectsMalformedIdBeforeHttpCall()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "");
        var service = new ArxivPaperLookupService(Client(handler));
        Assert.ThrowsAsync<ArgumentException>(async () => await service.GetByIdAsync("bad-id"));
        Assert.That(handler.CallCount, Is.Zero);
    }
}

}
