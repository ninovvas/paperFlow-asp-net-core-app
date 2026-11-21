namespace PaperFlow.Services.Providers.OpenAlex
{

    using System.Net;
    using System.Text.Json;
    using Microsoft.Extensions.Caching.Memory;
    using PaperFlow.Services.Interfaces.Providers;
    using PaperFlow.Services.Providers.Crossref;

    public sealed class OpenAlexAbstractService : IOpenAlexAbstractService
    {
        private readonly HttpClient httpClient;
        private readonly IMemoryCache cache;
        public OpenAlexAbstractService(HttpClient httpClient, IMemoryCache cache)
        {
            this.httpClient = httpClient;
            this.cache = cache;
        }

        public async Task<string?> GetAbstractByDoiAsync(string doi,
            CancellationToken cancellationToken = default)
        {
            string normalized = CrossrefDoi.Normalize(doi);
            string cacheKey = "PaperFlow:OpenAlex:abstract:" + normalized;
            if (cache.TryGetValue(cacheKey, out string? cached))
                return cached?.Length > 0 ? cached : null;

            string filter = "doi:https://doi.org/" + normalized;
            string uri = "works?filter=" + Uri.EscapeDataString(filter) + "&per_page=1&select=id,display_name,doi,abstract_inverted_index";
            using var response = await httpClient.GetAsync(uri,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                string? value = null;
                if (json.RootElement.TryGetProperty("results", out var works) &&
                    works.ValueKind == JsonValueKind.Array && works.GetArrayLength() > 0)
                {
                    var work = OpenAlexMapper.Parse(works[0]);
                    if (work is not null &&
                        string.Equals(work.Doi, normalized, StringComparison.OrdinalIgnoreCase))
                        value = work.Abstract;
                }
                cache.Set(cacheKey, value ?? string.Empty,
                    value is null ? TimeSpan.FromHours(8) : TimeSpan.FromDays(3));
                return value;
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException("OpenAlex returned invalid JSON.", ex);
            }
        }
    }
}

