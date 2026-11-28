namespace PaperFlow.Services.Tests.Providers
{
    using System.Net;
    using NUnit.Framework;
    using PaperFlow.Services.Models.Search;
    using PaperFlow.Services.Providers.Arxiv;
    using PaperFlow.Services.Tests.Infrastructure;


[TestFixture]
public sealed class ArxivSearchParsingCoverageTests
{
    private const string FullFeed = """
        <?xml version="1.0" encoding="utf-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom" xmlns:arxiv="http://arxiv.org/schemas/atom">
          <entry>
            <id>https://arxiv.org/abs/2401.01234v2</id>
            <title>  Mobile\n    Robot    Navigation </title>
            <summary>   A   useful\n summary </summary>
            <published>2024-01-12T12:00:00Z</published>
            <author><name>Alice Example</name></author>
            <author><name>Bob Example</name></author>
            <category term="cs.RO"/>
            <category term="cs.RO"/>
            <arxiv:doi>10.1234/example</arxiv:doi>
            <link title="pdf" href="https://arxiv.org/pdf/2401.01234v2"/>
          </entry>
        </feed>
        """;

    [Test]
    public async Task Search_ValidFeed_ParsesMetadataNormalizesIdAndWhitespace()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, FullFeed);
        using var client = new HttpClient(handler);
        var service = new ArxivSearchService(client);

        var results = await service.SearchAsync(new PaperSearchCriteria
        {
            Keywords = "robot",
            Start = 7,
            MaxResults = 5
        });

        Assert.That(results, Has.Count.EqualTo(1));
        var paper = results.Single();
        Assert.Multiple(() =>
        {
            Assert.That(paper.ExternalId, Is.EqualTo("2401.01234"));
            Assert.That(paper.Title, Does.Contain("Mobile"));
            Assert.That(paper.Title, Does.Contain("Robot"));
            Assert.That(paper.Abstract, Does.Contain("useful"));
            Assert.That(paper.PublicationDate, Is.EqualTo(new DateOnly(2024, 1, 12)));
            Assert.That(paper.Doi, Is.EqualTo("10.1234/example"));
            Assert.That(paper.Authors, Is.EquivalentTo(new[] { "Alice Example", "Bob Example" }));
            Assert.That(paper.Categories, Is.EquivalentTo(new[] { "cs.RO" }));
            Assert.That(paper.PdfUrl, Is.EqualTo("https://arxiv.org/pdf/2401.01234v2"));
            Assert.That(handler.RequestedUri?.Query, Does.Contain("start=7"));
        });
    }

    [Test]
    public async Task Search_OnlyWithPdf_SkipsEntriesWithoutPdf()
    {
        const string feed = """
            <feed xmlns="http://www.w3.org/2005/Atom">
              <entry><id>https://arxiv.org/abs/2401.11111</id><title>Paper without PDF</title></entry>
            </feed>
            """;
        using var client = new HttpClient(new StubHttpHandler(HttpStatusCode.OK, feed));
        var service = new ArxivSearchService(client);

        var results = await service.SearchAsync(new PaperSearchCriteria
        {
            Keywords = "robot",
            OnlyWithPdf = true
        });

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task Search_MissingEntryIdOrTitle_SkipsIncompleteRecords()
    {
        const string feed = """
            <feed xmlns="http://www.w3.org/2005/Atom">
              <entry><title>No ID</title></entry>
              <entry><id>https://arxiv.org/abs/2401.11111</id></entry>
            </feed>
            """;
        using var client = new HttpClient(new StubHttpHandler(HttpStatusCode.OK, feed));
        var service = new ArxivSearchService(client);

        var results = await service.SearchAsync("robot");
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Search_RejectsNegativeStartWithoutHttpRequest()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "");
        using var client = new HttpClient(handler);
        var service = new ArxivSearchService(client);

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.SearchAsync(new PaperSearchCriteria { Keywords = "robot", Start = -1 }));
        Assert.That(handler.CallCount, Is.Zero);
    }

    [Test]
    public void Search_RejectsMalformedXml()
    {
        using var client = new HttpClient(new StubHttpHandler(HttpStatusCode.OK, "<feed><unclosed>"));
        var service = new ArxivSearchService(client);

        Assert.ThrowsAsync<System.Xml.XmlException>(async () => await service.SearchAsync("robot"));
    }

    [Test]
    public void Search_RejectsDtdForSafeXmlParsing()
    {
        const string xml = "<!DOCTYPE feed [<!ENTITY unsafe 'danger'>]><feed xmlns='http://www.w3.org/2005/Atom'>&unsafe;</feed>";
        using var client = new HttpClient(new StubHttpHandler(HttpStatusCode.OK, xml));
        var service = new ArxivSearchService(client);

        Assert.ThrowsAsync<System.Xml.XmlException>(async () => await service.SearchAsync("robot"));
    }
}
}

