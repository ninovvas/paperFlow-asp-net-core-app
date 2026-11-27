namespace PaperFlow.Services.Tests.Providers
{
    using NUnit.Framework;
    using PaperFlow.Services.Providers.Crossref;
    using PaperFlow.Services.Providers.OpenAlex;
    using PaperFlow.Services.Providers.Arxiv;
    using PaperFlow.Services.Models.Search;


    [TestFixture]
    public class ProviderIdentifiersTests
    {
        [TestCase("10.1234/ABC", "10.1234/abc")]
        [TestCase("https://doi.org/10.5555/Book.X", "10.5555/book.x")]
        [TestCase("http://dx.doi.org/10.9999/A", "10.9999/a")]
        [TestCase("doi:10.1234/A", "10.1234/a")]
        public void Crossref_NormalizeDoi_AcceptsSupportedFormats(string input, string expected) =>
            Assert.That(CrossrefDoi.Normalize(input), Is.EqualTo(expected));

        [TestCase("")]
        [TestCase("not-a-doi")]
        [TestCase("11.1234/invalid")]
        [TestCase("10.12/short-prefix")]
        public void Crossref_NormalizeDoi_RejectsInvalidInput(string input) =>
            Assert.Throws<ArgumentException>(() => CrossrefDoi.Normalize(input));

        [TestCase("w123", "W123")]
        [TestCase("https://openalex.org/W987", "W987")]
        [TestCase(" W1 ", "W1")]
        public void OpenAlex_NormalizeWorkId_AcceptsSupportedFormats(string input, string expected) =>
            Assert.That(OpenAlexWorkId.Normalize(input), Is.EqualTo(expected));

        [TestCase("")]
        [TestCase("W")]
        [TestCase("X123")]
        [TestCase("https://example.com/W123")]
        public void OpenAlex_NormalizeWorkId_RejectsInvalidInput(string input) =>
            Assert.Throws<ArgumentException>(() => OpenAlexWorkId.Normalize(input));

        [Test]
        public void Arxiv_BuildQuery_RequiresSearchTerm() =>
            Assert.Throws<ArgumentException>(() => ArxivQueryBuilder.BuildRequestUrl(new PaperSearchCriteria()));

        [TestCase(0)]
        [TestCase(51)]
        public void Arxiv_BuildQuery_RejectsInvalidPageSize(int maxResults) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ArxivQueryBuilder.BuildRequestUrl(
                new PaperSearchCriteria { Keywords = "robot", MaxResults = maxResults }));

        [Test]
        public void Arxiv_BuildQuery_RejectsNegativeOffset() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ArxivQueryBuilder.BuildRequestUrl(
                new PaperSearchCriteria { Keywords = "robot", Start = -1 }));

        [TestCase("ti", "ti%3A")]
        [TestCase("abs", "abs%3A")]
        [TestCase("all", "all%3A")]
        public void Arxiv_BuildQuery_UsesSelectedField(string searchField, string encodedField)
        {
            var uri = ArxivQueryBuilder.BuildRequestUrl(new PaperSearchCriteria
            {
                Keywords = "mobile robot",
                SearchField = searchField,
                Start = 5,
                MaxResults = 7
            });
            Assert.Multiple(() =>
            {
                Assert.That(uri, Does.Contain(encodedField));
                Assert.That(uri, Does.Contain("start=5"));
                Assert.That(uri, Does.Contain("max_results=7"));
            });
        }

        [Test]
        public void Arxiv_BuildQuery_IncludesAuthorCategoryAndDateRange()
        {
            var uri = Uri.UnescapeDataString(ArxivQueryBuilder.BuildRequestUrl(new PaperSearchCriteria
            {
                Author = "Ada Lovelace",
                Category = "cs.RO",
                SubmittedFrom = new DateOnly(2024, 1, 1),
                SubmittedTo = new DateOnly(2024, 12, 31)
            }));
            Assert.Multiple(() =>
            {
                Assert.That(uri, Does.Contain("au:\"Ada Lovelace\""));
                Assert.That(uri, Does.Contain("cat:cs.RO"));
                Assert.That(uri, Does.Contain("submittedDate:[202401010000 TO 202412312359]"));
            });
        }

        [Test]
        public void Arxiv_BuildQuery_RejectsReversedDateRange() =>
            Assert.Throws<ArgumentException>(() => ArxivQueryBuilder.BuildRequestUrl(new PaperSearchCriteria
            {
                SubmittedFrom = new DateOnly(2026, 2, 1),
                SubmittedTo = new DateOnly(2026, 1, 1)
            }));
    }
}
