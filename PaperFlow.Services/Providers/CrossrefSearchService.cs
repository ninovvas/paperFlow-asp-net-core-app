
namespace PaperFlow.Services.Providers
{

    using System.Globalization;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;

    public sealed class CrossrefSearchService : IResearchPaperProvider
    {
        private readonly HttpClient httpClient;

        public string ProviderId => "crossref";
        public string ProviderName => "Crossref";

        public CrossrefSearchService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public Task<IReadOnlyList<PaperSearchResult>> SearchAsync(
            string query, int maxResults = 10,
            CancellationToken cancellationToken = default)
        {
            return SearchAsync(new PaperSearchCriteria
            {
                Keywords = query,
                MaxResults = maxResults
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<PaperSearchResult>> SearchAsync(
            PaperSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(criteria);

            if (criteria.MaxResults is < 1 or > 50)
                throw new ArgumentOutOfRangeException(nameof(criteria.MaxResults));
            if (criteria.Start < 0)
                throw new ArgumentOutOfRangeException(nameof(criteria.Start));
            if (criteria.SubmittedFrom > criteria.SubmittedTo)
                throw new ArgumentException("Start date must not be after end date.");

            string requestUri = BuildRequestUri(criteria);
            using var response = await httpClient.GetAsync(
                requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                using var document = await JsonDocument.ParseAsync(stream,
                    cancellationToken: cancellationToken);

                if (!document.RootElement.TryGetProperty("message", out var message) ||
                    !message.TryGetProperty("items", out var items) ||
                    items.ValueKind != JsonValueKind.Array)
                    throw new HttpRequestException("Crossref returned an unexpected JSON structure.");

                var results = new List<PaperSearchResult>();
                foreach (var item in items.EnumerateArray())
                {
                    var paper = ParsePaper(item);
                    if (paper is null)
                        continue;
                    if (criteria.OnlyWithPdf && string.IsNullOrWhiteSpace(paper.PdfUrl))
                        continue;
                    results.Add(paper);
                }
                return results;
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException("Crossref returned invalid JSON.", ex);
            }
        }

        private static string BuildRequestUri(PaperSearchCriteria criteria)
        {
            var parameters = new List<KeyValuePair<string, string>>
        {
            new("rows", criteria.MaxResults.ToString(CultureInfo.InvariantCulture)),
            new("offset", criteria.Start.ToString(CultureInfo.InvariantCulture)),
            new("select", "DOI,title,abstract,author,published,published-print,published-online,created,URL,link,subject"),
        };

            if (!string.IsNullOrWhiteSpace(criteria.Keywords))
            {
                string queryField = criteria.SearchField switch
                {
                    "ti" => "query.title",
                    "abs" => "query",
                    _ => "query.bibliographic"
                };
                parameters.Add(new(queryField, criteria.Keywords.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(criteria.Author))
                parameters.Add(new("query.author", criteria.Author.Trim()));

            // arXiv category identifiers such as cs.RO are not Crossref subject taxonomy.
            // They must not be submitted as if they were compatible filters.
            if (!string.IsNullOrWhiteSpace(criteria.Category) &&
                !Regex.IsMatch(criteria.Category.Trim(), @"^[a-z]+\.[A-Za-z0-9-]+$"))
            {
                parameters.Add(new("query", criteria.Category.Trim()));
            }

            var dateFilters = new List<string>();
            // Crossref indexes publication dates, not arXiv submission dates.
            if (criteria.SubmittedFrom is { } from)
                dateFilters.Add("from-pub-date:" + from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (criteria.SubmittedTo is { } to)
                dateFilters.Add("until-pub-date:" + to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (dateFilters.Count > 0)
                parameters.Add(new("filter", string.Join(",", dateFilters)));

            if (criteria.SortBy == "submittedDate")
            {
                parameters.Add(new("sort", "published"));
                parameters.Add(new("order", criteria.SortOrder == "ascending" ? "asc" : "desc"));
            }
            else if (criteria.SortBy == "lastUpdatedDate")
            {
                parameters.Add(new("sort", "updated"));
                parameters.Add(new("order", criteria.SortOrder == "ascending" ? "asc" : "desc"));
            }
            else if (criteria.SortOrder == "ascending")
            {
                parameters.Add(new("order", "asc"));
            }

            if (!parameters.Any(p => p.Key.StartsWith("query", StringComparison.Ordinal)) &&
                dateFilters.Count == 0)
                throw new ArgumentException("Enter a keyword, author, or publication date filter.");

            return "works?" + string.Join("&", parameters.Select(p =>
                Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
        }

        private static PaperSearchResult? ParsePaper(JsonElement item)
        {
            string? doi = GetString(item, "DOI");
            string? title = FirstString(item, "title");
            if (string.IsNullOrWhiteSpace(doi) || string.IsNullOrWhiteSpace(title))
                return null;

            string? publicationUrl = GetString(item, "URL");
            if (!Uri.TryCreate(publicationUrl, UriKind.Absolute, out var checkedUrl) ||
                checkedUrl.Scheme != Uri.UriSchemeHttps)
            {
                publicationUrl = "https://doi.org/" + doi;
            }

            return new PaperSearchResult
            {
                ExternalId = doi.Trim().ToLowerInvariant(),
                Doi = doi.Trim(),
                Title = CleanText(title),
                Abstract = CleanText(GetString(item, "abstract")),
                PublicationDate = ParseDate(item),
                PublicationUrl = publicationUrl,
                PdfUrl = FindPdfUrl(item),
                SourceName = "Crossref",
                Authors = GetAuthors(item),
                Categories = GetStringArray(item, "subject")
            };
        }

        private static string? GetString(JsonElement item, string property)
        {
            return item.ValueKind == JsonValueKind.Object &&
                   item.TryGetProperty(property, out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string? FirstString(JsonElement item, string property)
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty(property, out var values) ||
                values.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var value in values.EnumerateArray())
                if (value.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(value.GetString()))
                    return value.GetString();
            return null;
        }

        private static List<string> GetStringArray(JsonElement item, string property)
        {
            var values = new List<string>();
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty(property, out var array) ||
                array.ValueKind != JsonValueKind.Array)
                return values;

            foreach (var value in array.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.String)
                    continue;
                string text = CleanText(value.GetString());
                if (!string.IsNullOrEmpty(text))
                    values.Add(text);
            }
            return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> GetAuthors(JsonElement item)
        {
            var authors = new List<string>();
            if (!item.TryGetProperty("author", out var array) ||
                array.ValueKind != JsonValueKind.Array)
                return authors;

            foreach (var author in array.EnumerateArray())
            {
                string name = string.Join(" ", new[]
                {
                GetString(author, "given"), GetString(author, "family")
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (string.IsNullOrWhiteSpace(name))
                    name = GetString(author, "name") ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name))
                    authors.Add(CleanText(name));
            }
            return authors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static DateOnly? ParseDate(JsonElement item)
        {
            foreach (var property in new[] { "published", "published-online", "published-print", "created" })
            {
                if (!item.TryGetProperty(property, out var date) ||
                    date.ValueKind != JsonValueKind.Object ||
                    !date.TryGetProperty("date-parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array || parts.GetArrayLength() == 0)
                    continue;

                var components = parts[0];
                if (components.ValueKind != JsonValueKind.Array || components.GetArrayLength() == 0)
                    continue;

                int year = components[0].GetInt32();
                int month = components.GetArrayLength() > 1 ? components[1].GetInt32() : 1;
                int day = components.GetArrayLength() > 2 ? components[2].GetInt32() : 1;
                if (year is >= 1 and <= 9999 && month is >= 1 and <= 12 &&
                    day >= 1 && day <= DateTime.DaysInMonth(year, month))
                    return new DateOnly(year, month, day);
            }
            return null;
        }

        private static string? FindPdfUrl(JsonElement item)
        {
            if (!item.TryGetProperty("link", out var links) || links.ValueKind != JsonValueKind.Array)
                return null;
            foreach (var link in links.EnumerateArray())
            {
                string? url = GetString(link, "URL");
                string? contentType = GetString(link, "content-type");
                if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) ||
                    !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps)
                    continue;
                return url;
            }
            return null;
        }

        private static string CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            // Metadata abstracts may contain XML/JATS markup. Return readable plain text;
            // Razor still HTML encodes the final value when rendering it.
            return Regex.Replace(Regex.Replace(value, "<[^>]*>", " "), @"\s+", " ").Trim();
        }
    }

}

