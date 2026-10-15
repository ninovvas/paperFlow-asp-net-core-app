using PaperFlow.Services.Interfaces;
using PaperFlow.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PaperFlow.Services.Providers
{
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

        public ArxivSearchService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IReadOnlyList<PaperSearchResult>> SearchAsync(
            string query,
            int maxResults = 10,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException(
                    "Search query cannot be empty.",
                    nameof(query));
            }

            if (maxResults < 1 || maxResults > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxResults),
                    "Max results must be between 1 and 100.");
            }

            // Search as a phrase across arXiv fields.
            string normalizedQuery = query.Trim().Replace("\"", "");

            string searchExpression =
                $"all:\"{normalizedQuery}\"";

            string requestUrl =
                $"{ApiUrl}?search_query=" +
                Uri.EscapeDataString(searchExpression) +
                $"&start=0&max_results={maxResults}" +
                "&sortBy=relevance&sortOrder=descending";

            using var response = await httpClient.GetAsync(
                requestUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            var xmlSettings = new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var reader = XmlReader.Create(
                stream,
                xmlSettings);

            XDocument document = await XDocument.LoadAsync(
                reader,
                LoadOptions.None,
                cancellationToken);

            return document
                .Descendants(Atom + "entry")
                .Select(ParsePaper)
                .ToList();
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
            // duplicate detection, e.g. 2401.12345v2.

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
    }
}
