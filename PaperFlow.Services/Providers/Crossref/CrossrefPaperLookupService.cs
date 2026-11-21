namespace PaperFlow.Services.Providers.Crossref
{
    using System.Net;
    using System.Text.Json;
    using PaperFlow.Services.Interfaces.Providers;
    using PaperFlow.Services.Models;

    public sealed class CrossrefPaperLookupService : ICrossrefPaperLookupService
{
    private readonly HttpClient httpClient;
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTime lastRequestUtc = DateTime.MinValue;

    public CrossrefPaperLookupService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<PaperSearchResult?> GetByDoiAsync(
        string doi, CancellationToken cancellationToken = default)
    {
        string id = CrossrefDoi.Normalize(doi);
        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            TimeSpan delay = lastRequestUtc.AddMilliseconds(300) - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);

            lastRequestUtc = DateTime.UtcNow;
            using var response = await httpClient.GetAsync(
                "works/" + Uri.EscapeDataString(id),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                using var document = await JsonDocument.ParseAsync(body,
                    cancellationToken: cancellationToken);
                if (!document.RootElement.TryGetProperty("message", out var item))
                    throw new HttpRequestException("Crossref response does not contain publication metadata.");

                var result = CrossrefSearchService.ParsePaper(item);
                return result != null && CrossrefDoi.Normalize(result.Doi!) == id
                    ? result
                    : null;
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException("Crossref returned invalid JSON.", ex);
            }
        }
        finally
        {
            RequestGate.Release();
        }
    }
}
}
