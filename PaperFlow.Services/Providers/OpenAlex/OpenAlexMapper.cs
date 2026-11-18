namespace PaperFlow.Services.Providers.OpenAlex
{
    using System.Globalization;
    using System.Text.Json;
    using PaperFlow.Services.Models;
    using PaperFlow.Services.Providers.Crossref;

    internal static class OpenAlexMapper
{
    public static PaperSearchResult? Parse(JsonElement work)
    {
        string? workId = GetString(work, "id");
        string? title = GetString(work, "display_name");
        if (string.IsNullOrWhiteSpace(workId) || string.IsNullOrWhiteSpace(title))
            return null;

        string id;
        try { id = OpenAlexWorkId.Normalize(workId); }
        catch (ArgumentException) { return null; }

        string? doi = GetString(work, "doi");
        if (!string.IsNullOrWhiteSpace(doi))
        {
            try { doi = CrossrefDoi.Normalize(doi); }
            catch (ArgumentException) { doi = null; }
        }

        JsonElement primaryLocation = GetObject(work, "primary_location");
        JsonElement bestLocation = GetObject(work, "best_oa_location");
        JsonElement primarySource = GetObject(primaryLocation, "source");
        string? publicationUrl = SecureUrl(GetString(primaryLocation, "landing_page_url"))
            ?? SecureUrl(GetString(work, "doi"))
            ?? $"https://openalex.org/{id}";
        string? pdfUrl = SecureUrl(GetString(bestLocation, "pdf_url"))
            ?? SecureUrl(GetString(primaryLocation, "pdf_url"));
        string? dateText = GetString(work, "publication_date");
        DateOnly? publicationDate = DateOnly.TryParseExact(dateText, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

        var result = new PaperSearchResult
        {
            ExternalId = id,
            Title = title.Trim(),
            Doi = doi,
            Abstract = ReadAbstract(GetObject(work, "abstract_inverted_index")),
            AbstractSource = "OpenAlex",
            PublicationDate = publicationDate,
            PublicationUrl = publicationUrl,
            PdfUrl = pdfUrl,
            SourceName = "OpenAlex",
            PublicationVenue = GetString(primarySource, "display_name"),
            PublicationType = GetString(work, "type")?.Replace('-', ' '),
            CitationCount = GetInt(work, "cited_by_count")
        };
        if (string.IsNullOrEmpty(result.Abstract))
            result.AbstractSource = null;

        if (work.TryGetProperty("authorships", out var authors) && authors.ValueKind == JsonValueKind.Array)
        {
            foreach (var authorship in authors.EnumerateArray())
            {
                string? name = GetString(GetObject(authorship, "author"), "display_name");
                if (!string.IsNullOrWhiteSpace(name))
                    result.Authors.Add(name.Trim());
            }
        }

        if (work.TryGetProperty("topics", out var topics) && topics.ValueKind == JsonValueKind.Array)
        {
            foreach (var topic in topics.EnumerateArray().Take(5))
            {
                string? name = GetString(topic, "display_name");
                if (!string.IsNullOrWhiteSpace(name)) result.Categories.Add(name.Trim());
            }
        }
        if (result.Categories.Count == 0)
        {
            string? name = GetString(GetObject(work, "primary_topic"), "display_name");
            if (!string.IsNullOrWhiteSpace(name)) result.Categories.Add(name.Trim());
        }
        result.Authors = result.Authors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        result.Categories = result.Categories.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return result;
    }

    public static string? ReadAbstract(JsonElement invertedIndex)
    {
        if (invertedIndex.ValueKind != JsonValueKind.Object)
            return null;

        var words = new SortedDictionary<int, string>();
        foreach (var entry in invertedIndex.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Array) continue;
            foreach (var location in entry.Value.EnumerateArray())
            {
                if (location.ValueKind != JsonValueKind.Number ||
                    !location.TryGetInt32(out int index) || index < 0 || index > 5000)
                    continue;
                words.TryAdd(index, entry.Name);
            }
        }
        if (words.Count == 0) return null;
        // Malformed indices must not allocate excessively large arrays.
        if (words.Keys.Max() >= 5000) return null;
        string result = string.Join(" ", Enumerable.Range(0, words.Keys.Max() + 1)
            .Select(i => words.TryGetValue(i, out var word) ? word : string.Empty));
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
        return result.Length > 10000 ? result[..10000] : result;
    }

    private static JsonElement GetObject(JsonElement item, string name) =>
        item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var found)
        && found.ValueKind == JsonValueKind.Object ? found : default;

    private static string? GetString(JsonElement item, string name) =>
        item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int? GetInt(JsonElement item, string name) =>
        item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number)
        && number >= 0 ? number : null;

    private static string? SecureUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.AbsoluteUri : null;
}
}
