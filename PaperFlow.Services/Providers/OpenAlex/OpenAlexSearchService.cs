

namespace PaperFlow.Services.Providers
{
    using System.Globalization;
    using System.Net;
    using System.Text.Json;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;


public sealed class OpenAlexSearchService : IResearchPaperProvider
{
    private readonly HttpClient httpClient;
    public string ProviderId => "openalex";
    public string ProviderName => "OpenAlex";

    public OpenAlexSearchService(HttpClient httpClient) => this.httpClient = httpClient;

    public Task<IReadOnlyList<PaperSearchResult>> SearchAsync(string query, int maxResults = 10,
        CancellationToken cancellationToken = default) =>
        SearchAsync(new PaperSearchCriteria { Keywords = query, MaxResults = maxResults }, cancellationToken);

    public async Task<IReadOnlyList<PaperSearchResult>> SearchAsync(PaperSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        if (criteria.MaxResults is < 1 or > 50 || criteria.Start < 0)
            throw new ArgumentOutOfRangeException(nameof(criteria), "Invalid result range.");
        if (criteria.SubmittedFrom > criteria.SubmittedTo)
            throw new ArgumentException("Start date must not be after end date.");

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("per_page", criteria.MaxResults.ToString(CultureInfo.InvariantCulture)),
            new("page", (criteria.Start / criteria.MaxResults + 1).ToString(CultureInfo.InvariantCulture)),
            new("select", "id,display_name,doi,publication_date,abstract_inverted_index,authorships,topics,primary_topic,primary_location,best_oa_location,cited_by_count,type")
        };
        string queryText = string.Join(" ", new[] { criteria.Keywords, criteria.Author }
            .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (string.IsNullOrWhiteSpace(queryText) && !criteria.SubmittedFrom.HasValue &&
            !criteria.SubmittedTo.HasValue)
            throw new ArgumentException("Enter keywords, an author, or a publication date filter.");

        if (queryText.Length > 0) parameters.Add(new("search", queryText));
        var filters = new List<string>();
        if (criteria.SubmittedFrom is { } from)
            filters.Add("from_publication_date:" + from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (criteria.SubmittedTo is { } to)
            filters.Add("to_publication_date:" + to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (filters.Count > 0) parameters.Add(new("filter", string.Join(",", filters)));

        if (criteria.SortBy is "submittedDate" or "lastUpdatedDate")
        {
            // Do not sort on the paid-only updated_date property.
            parameters.Add(new("sort", "publication_date:" +
                (criteria.SortOrder == "ascending" ? "asc" : "desc")));
        }
        // OpenAlex does not use arXiv subject codes such as cs.RO.
        // Text and author terms are searched jointly; arXiv-only filters are not sent.

        string uri = "works?" + string.Join("&", parameters.Select(p =>
            Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
        using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        try
        {
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array)
                throw new HttpRequestException("OpenAlex returned an unexpected response.");
            return results.EnumerateArray()
                .Select(OpenAlexMapper.Parse)
                .Where(p => p is not null &&
                    (!criteria.OnlyWithPdf || !string.IsNullOrWhiteSpace(p.PdfUrl)))
                .Select(p => p!)
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException("OpenAlex returned invalid JSON.", ex);
        }
    }
}
}

