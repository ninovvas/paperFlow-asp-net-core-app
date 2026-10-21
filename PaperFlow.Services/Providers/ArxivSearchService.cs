namespace PaperFlow.Services.Providers
{
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    public class ArxivSearchService : IResearchPaperProvider
    {
        private const string ApiUrl =
            "https://export.arxiv.org/api/query";

        private static readonly XNamespace Atom =
            "http://www.w3.org/2005/Atom";

        private static readonly XNamespace Arxiv =
            "http://arxiv.org/schemas/atom";

        private readonly HttpClient httpClient;

        public string ProviderName => "arXiv";
        public string ProviderId => "arxiv";

        public ArxivSearchService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }


        private static PaperSearchResult ParsePaper(
            XElement entry)
        {
            string entryUrl =
                entry.Element(Atom + "id")?.Value.Trim()
                ?? string.Empty;

            string externalId = entryUrl
                .TrimEnd('/')
                .Split('/')
                .Last();

            // Remove arXiv version suffix for
            // duplicate detection

            externalId = System.Text.RegularExpressions.Regex
                .Replace(externalId, @"v\d+$", "");

            var publishedText =
                entry.Element(Atom + "published")?.Value;

            DateOnly? publicationDate = null;

            if (DateTimeOffset.TryParse(
                publishedText,
                out var parsedDate))
            {
                publicationDate =
                    DateOnly.FromDateTime(parsedDate.Date);
            }

            string? pdfUrl = entry
                .Elements(Atom + "link")
                .FirstOrDefault(link =>
                    (string?)link.Attribute("title") == "pdf")
                ?.Attribute("href")?.Value;

            string? doi =
                entry.Element(Arxiv + "doi")?.Value.Trim();

            var authors = entry
                .Elements(Atom + "author")
                .Select(author =>
                    author.Element(Atom + "name")?.Value.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToList();

            var categories = entry
                .Elements(Atom + "category")
                .Select(category =>
                    (string?)category.Attribute("term"))
                .Where(category =>
                    !string.IsNullOrWhiteSpace(category))
                .Select(category => category!)
                .Distinct()
                .ToList();

            return new PaperSearchResult
            {
                ExternalId = externalId,

                Title = NormalizeWhitespace(
                    entry.Element(Atom + "title")?.Value),

                Abstract = NormalizeWhitespace(
                    entry.Element(Atom + "summary")?.Value),

                PublicationDate = publicationDate,

                Doi = doi,

                PublicationUrl = entryUrl,

                PdfUrl = pdfUrl,

                SourceName = "arXiv",

                Authors = authors,

                Categories = categories
            };
        }

        private static string NormalizeWhitespace(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return System.Text.RegularExpressions.Regex
                .Replace(value.Trim(), @"\s+", " ");
        }

        public Task<IReadOnlyList<PaperSearchResult>> SearchAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
        {
            var criteria = new PaperSearchCriteria
            {
                Keywords = query,
                SearchField = "all",
                SortBy = "relevance",
                SortOrder = "descending",
                MaxResults = maxResults
            };

            return SearchAsync(criteria, cancellationToken);
        }

        public async Task<IReadOnlyList<PaperSearchResult>> SearchAsync(PaperSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            // Respect the offset even if the existing query builder does not
            // yet expose arXiv's "start" query parameter.
            if (criteria.Start < 0)
                throw new ArgumentOutOfRangeException(nameof(criteria.Start));

            string url = ArxivQueryBuilder.BuildRequestUrl(criteria);
            string startParameter = "start=" +
                criteria.Start.ToString(System.Globalization.CultureInfo.InvariantCulture);
            url = System.Text.RegularExpressions.Regex.IsMatch(
                    url, @"([?&])start=\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                ? System.Text.RegularExpressions.Regex.Replace(
                    url, @"([?&])start=\d+", "$1" + startParameter,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                : url + (url.Contains('?') ? "&" : "?") + startParameter;

            using var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            var settings = new System.Xml.XmlReaderSettings
            {
                Async = true,
                DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var reader = System.Xml.XmlReader.Create(
                stream, settings);

            var document = await System.Xml.Linq.XDocument.LoadAsync(
                reader,
                System.Xml.Linq.LoadOptions.None,
                cancellationToken);

            var entries = document
                .Descendants(
                    System.Xml.Linq.XNamespace.Get(
                        "http://www.w3.org/2005/Atom") + "entry");

            var results = entries
                .Select(ParsePaper)
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.ExternalId) &&
                    !string.IsNullOrWhiteSpace(p.Title))
                .ToList();

            if (criteria.OnlyWithPdf)
            {
                results = results
                    .Where(p =>
                        !string.IsNullOrWhiteSpace(p.PdfUrl))
                    .ToList();
            }

            return results;
        }
    }
}
