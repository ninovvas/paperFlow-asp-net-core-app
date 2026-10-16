namespace PaperFlow.Services.Providers
{

    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;

    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;

    public class ArxivPaperLookupService
        : IArxivPaperLookupService
    {
        private const string ApiUrl =
            "https://export.arxiv.org/api/query";

        private static readonly XNamespace Atom =
            "http://www.w3.org/2005/Atom";

        private static readonly XNamespace Arxiv =
            "http://arxiv.org/schemas/atom";

        private static readonly Regex ArxivIdPattern = new(
            @"^(?:\d{4}\.\d{4,5}|[a-z\-]+(?:\.[A-Z]{2})?/\d{7})$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly HttpClient httpClient;

        public ArxivPaperLookupService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<PaperSearchResult?> GetByIdAsync(
            string arxivId,
            CancellationToken cancellationToken = default)
        {
            string normalizedId = NormalizeId(arxivId);

            string requestUrl = $"{ApiUrl}?id_list=" +
                Uri.EscapeDataString(normalizedId);

            using var response = await httpClient.GetAsync(
                requestUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            var settings = new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var reader = XmlReader.Create(stream, settings);

            XDocument document = await XDocument.LoadAsync(
                reader,
                LoadOptions.None,
                cancellationToken);

            XElement? entry = document
                .Descendants(Atom + "entry")
                .FirstOrDefault();

            if (entry == null)
            {
                return null;
            }

            string entryUrl =
                entry.Element(Atom + "id")?.Value.Trim()
                ?? string.Empty;

            if (!Uri.TryCreate(
                entryUrl,
                UriKind.Absolute,
                out Uri? publicationUri))
            {
                return null;
            }

            string responsePath = publicationUri.AbsolutePath;

            if (!responsePath.StartsWith(
                "/abs/",
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string responseId = NormalizeId(
                Uri.UnescapeDataString(
                    responsePath["/abs/".Length..]));

            // arXiv can return a feed entry containing
            // an error message for an invalid or unknown ID.
            if (!string.Equals(
                responseId,
                normalizedId,
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string title = Clean(
                entry.Element(Atom + "title")?.Value);

            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            string published =
                entry.Element(Atom + "published")?.Value
                ?? string.Empty;

            DateOnly? publicationDate = null;

            if (DateTimeOffset.TryParse(
                published,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
            {
                publicationDate =
                    DateOnly.FromDateTime(parsedDate.UtcDateTime);
            }

            string? pdfUrl = entry
                .Elements(Atom + "link")
                .Where(e =>
                    (string?)e.Attribute("title") == "pdf")
                .Select(e =>
                    (string?)e.Attribute("href"))
                .FirstOrDefault();

            var authors = entry
                .Elements(Atom + "author")
                .Select(e => Clean(
                    e.Element(Atom + "name")?.Value))
                .Where(name => name.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var categories = entry
                .Elements(Atom + "category")
                .Select(e =>
                    (string?)e.Attribute("term"))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string? doi =
                entry.Element(Arxiv + "doi")?.Value.Trim();

            return new PaperSearchResult
            {
                ExternalId = normalizedId,
                Title = title,
                Abstract = Clean(
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

        public static string NormalizeId(string arxivId)
        {
            if (string.IsNullOrWhiteSpace(arxivId))
            {
                throw new ArgumentException(
                    "arXiv ID cannot be empty.",
                    nameof(arxivId));
            }

            string id = arxivId.Trim();

            // Accept an arXiv abstract URL or a plain ID.
            if (Uri.TryCreate(
                id,
                UriKind.Absolute,
                out Uri? uri))
            {
                if (uri.Host != "arxiv.org" ||
                    !uri.AbsolutePath.StartsWith(
                        "/abs/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "Expected an arXiv abstract URL.");
                }

                id = Uri.UnescapeDataString(
                    uri.AbsolutePath["/abs/".Length..]);
            }

            id = Regex.Replace(
                id,
                @"v\d+$",
                "",
                RegexOptions.IgnoreCase);

            if (!ArxivIdPattern.IsMatch(id))
            {
                throw new ArgumentException(
                    "Invalid arXiv ID format.",
                    nameof(arxivId));
            }

            return id;
        }

        private static string Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : Regex.Replace(value.Trim(), @"\s+", " ");
        }
    }
}

