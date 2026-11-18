namespace PaperFlow.Services.Providers.OpenAlex
{
    using System.Net;
    using System.Text.Json;
    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Models;

    public sealed class OpenAlexWorkLookupService : IOpenAlexWorkLookupService
{
    private readonly HttpClient httpClient;
    public OpenAlexWorkLookupService(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<PaperSearchResult?> GetByIdAsync(string workId,
        CancellationToken cancellationToken = default)
    {
        string id = OpenAlexWorkId.Normalize(workId);
        using var response = await httpClient.GetAsync("works/" + id,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        try
        {
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var record = OpenAlexMapper.Parse(json.RootElement);
            return record?.ExternalId == id ? record : null;
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException("OpenAlex returned invalid JSON.", ex);
        }
    }
}
}
