namespace PaperFlow.Services.Tests.Providers
{
    using System.Net;
    using NUnit.Framework;
    using PaperFlow.Services.Models.Search;
    using PaperFlow.Services.Providers.Crossref;
    using PaperFlow.Services.Providers.OpenAlex;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public class ProviderHttpTests
{
    private static HttpClient Client(StubHttpHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://api.example.test/")
    };

    [Test]
    public async Task CrossrefSearch_ParsesValidResponse()
    {
        const string json = """
        {"message":{"items":[{"DOI":"10.1234/robot","title":["Mobile robot navigation"],"publisher":"Example Publisher","author":[{"given":"Ada","family":"Lovelace"}],"is-referenced-by-count":7}]}}
        """;
        var handler = new StubHttpHandler(HttpStatusCode.OK, json);
        var results = await new CrossrefSearchService(Client(handler))
            .SearchAsync(new PaperSearchCriteria { Keywords = "robot" });
        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Doi, Is.EqualTo("10.1234/robot"));
            Assert.That(results[0].Authors, Does.Contain("Ada Lovelace"));
            Assert.That(results[0].CitationCount, Is.EqualTo(7));
            Assert.That(handler.RequestedUri!.Query, Does.Contain("rows=20"));
        });
    }

    [Test]
    public async Task CrossrefSearch_InvalidRecordsAreSkipped()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK,
            """{"message":{"items":[{"DOI":"10.1234/valid","title":["Valid title"]},{"title":["Missing DOI"]}]}}""");
        var result = await new CrossrefSearchService(Client(handler))
            .SearchAsync("robot");
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CrossrefSearch_OnlyWithPdfFiltersRecords()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK,
            """{"message":{"items":[{"DOI":"10.1234/valid","title":["No PDF"]}]}}""");
        var result = await new CrossrefSearchService(Client(handler))
            .SearchAsync(new PaperSearchCriteria { Keywords = "robot", OnlyWithPdf = true });
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void CrossrefSearch_HttpErrorIncludesProviderDetails()
    {
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, "invalid request");
        var service = new CrossrefSearchService(Client(handler));
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () => await service.SearchAsync("robot"));
        Assert.That(ex!.Message, Does.Contain("invalid request"));
    }

    [Test]
    public void CrossrefSearch_InvalidJsonThrowsHttpRequestException()
    {
        var service = new CrossrefSearchService(Client(
            new StubHttpHandler(HttpStatusCode.OK, "not-json")));
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.SearchAsync("robot"));
    }

    [Test]
    public void CrossrefSearch_UnrecognizedJsonShapeThrowsHttpRequestException()
    {
        var service = new CrossrefSearchService(Client(
            new StubHttpHandler(HttpStatusCode.OK, "{}")));
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.SearchAsync("robot"));
    }

    [TestCase(0)]
    [TestCase(51)]
    public void CrossrefSearch_RejectsInvalidPageSize(int count)
    {
        var service = new CrossrefSearchService(Client(new StubHttpHandler(HttpStatusCode.OK, "{}")));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.SearchAsync(
            new PaperSearchCriteria { Keywords = "robot", MaxResults = count }));
    }

    [Test]
    public async Task CrossrefLookup_404ReturnsNull()
    {
        var service = new CrossrefPaperLookupService(Client(
            new StubHttpHandler(HttpStatusCode.NotFound, "{}")));
        Assert.That(await service.GetByDoiAsync("10.1234/missing"), Is.Null);
    }

    [Test]
    public async Task CrossrefLookup_ValidRecordReturnsPaper()
    {
        var service = new CrossrefPaperLookupService(Client(
            new StubHttpHandler(HttpStatusCode.OK,
                """{"message":{"DOI":"10.1234/matched","title":["Matched paper"]}}""")));
        var paper = await service.GetByDoiAsync("10.1234/matched");
        Assert.That(paper?.Title, Is.EqualTo("Matched paper"));
    }

    [Test]
    public async Task OpenAlexSearch_ParsesResponse()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK,
            """{"results":[{"id":"https://openalex.org/W123","display_name":"Robot learning","doi":"https://doi.org/10.1234/test","publication_date":"2024-02-01","cited_by_count":12}]}""");
        var service = new OpenAlexSearchService(Client(handler));
        var results = await service.SearchAsync(new PaperSearchCriteria { Keywords = "robot" });
        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].ExternalId, Is.EqualTo("W123"));
            Assert.That(results[0].Doi, Is.EqualTo("10.1234/test"));
            Assert.That(results[0].CitationCount, Is.EqualTo(12));
            Assert.That(handler.RequestedUri!.Query, Does.Contain("search=robot"));
        });
    }

    [Test]
    public async Task OpenAlexSearch_OnlyWithPdfFiltersRecords()
    {
        var service = new OpenAlexSearchService(Client(new StubHttpHandler(HttpStatusCode.OK,
            """{"results":[{"id":"https://openalex.org/W123","display_name":"No PDF"}]}""")));
        var results = await service.SearchAsync(new PaperSearchCriteria
        { Keywords = "robot", OnlyWithPdf = true });
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void OpenAlexSearch_InvalidJsonThrowsHttpRequestException()
    {
        var service = new OpenAlexSearchService(Client(
            new StubHttpHandler(HttpStatusCode.OK, "broken")));
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.SearchAsync("robot"));
    }

    [Test]
    public void OpenAlexSearch_MissingResultsThrowsHttpRequestException()
    {
        var service = new OpenAlexSearchService(Client(
            new StubHttpHandler(HttpStatusCode.OK, "{}")));
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.SearchAsync("robot"));
    }

    [Test]
    public async Task OpenAlexLookup_404ReturnsNull()
    {
        var service = new OpenAlexWorkLookupService(Client(
            new StubHttpHandler(HttpStatusCode.NotFound, "{}")));
        Assert.That(await service.GetByIdAsync("W123"), Is.Null);
    }

    [Test]
    public async Task OpenAlexLookup_ValidRecordReturnsPaper()
    {
        var service = new OpenAlexWorkLookupService(Client(
            new StubHttpHandler(HttpStatusCode.OK,
                """{"id":"https://openalex.org/W123","display_name":"Valid work"}""")));
        Assert.That((await service.GetByIdAsync("W123"))?.Title, Is.EqualTo("Valid work"));
    }

    [Test]
    public async Task OpenAlexLookup_MismatchedRecordReturnsNull()
    {
        var service = new OpenAlexWorkLookupService(Client(
            new StubHttpHandler(HttpStatusCode.OK,
                """{"id":"https://openalex.org/W999","display_name":"Other work"}""")));
        Assert.That(await service.GetByIdAsync("W123"), Is.Null);
    }
}
}

