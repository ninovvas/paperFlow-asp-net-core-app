namespace PaperFlow.Services.Tests.Providers
{
    using System.Net;
    using NUnit.Framework;
    using PaperFlow.Services.Models.Search;
    using PaperFlow.Services.Providers.Crossref;
    using PaperFlow.Services.Providers.OpenAlex;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public class ProviderValidationTests
{
    private static HttpClient Client(StubHttpHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://api.example.test/")
    };

    [TestCase(-1, 10)]
    [TestCase(0, 0)]
    [TestCase(0, 51)]
    public void OpenAlex_RejectsInvalidRange(int start, int maxResults)
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "{}");
        var service = new OpenAlexSearchService(Client(handler));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.SearchAsync(
            new PaperSearchCriteria { Keywords = "robot", Start = start, MaxResults = maxResults }));
        Assert.That(handler.CallCount, Is.Zero);
    }

    [Test]
    public void OpenAlex_RejectsEmptySearch()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "{}");
        var service = new OpenAlexSearchService(Client(handler));
        Assert.ThrowsAsync<ArgumentException>(async () => await service.SearchAsync(new PaperSearchCriteria()));
        Assert.That(handler.CallCount, Is.Zero);
    }

    [Test]
    public void OpenAlex_RejectsReversedDates()
    {
        var service = new OpenAlexSearchService(Client(new StubHttpHandler(HttpStatusCode.OK, "{}")));
        Assert.ThrowsAsync<ArgumentException>(async () => await service.SearchAsync(new PaperSearchCriteria
        {
            Keywords = "robot",
            SubmittedFrom = new DateOnly(2026, 2, 1),
            SubmittedTo = new DateOnly(2026, 1, 1)
        }));
    }

    [Test]
    public void Crossref_RejectsNegativeStart()
    {
        var service = new CrossrefSearchService(Client(new StubHttpHandler(HttpStatusCode.OK, "{}")));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.SearchAsync(
            new PaperSearchCriteria { Keywords = "robot", Start = -1 }));
    }

    [Test]
    public void Crossref_RejectsReversedDates()
    {
        var service = new CrossrefSearchService(Client(new StubHttpHandler(HttpStatusCode.OK, "{}")));
        Assert.ThrowsAsync<ArgumentException>(async () => await service.SearchAsync(new PaperSearchCriteria
        {
            Keywords = "robot",
            SubmittedFrom = new DateOnly(2026, 2, 1),
            SubmittedTo = new DateOnly(2026, 1, 1)
        }));
    }
}
}

