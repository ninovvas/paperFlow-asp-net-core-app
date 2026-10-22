namespace PaperFlow.Services.Http
{
    using System.Net;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    public sealed class ArxivResilienceHandler : DelegatingHandler
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTimeOffset nextAllowed = DateTimeOffset.MinValue;
    private static readonly TimeSpan MinimumGap = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxCooldown = TimeSpan.FromMinutes(30);

    private readonly IMemoryCache cache;
    private readonly ILogger<ArxivResilienceHandler> logger;

    public ArxivResilienceHandler(IMemoryCache cache, ILogger<ArxivResilienceHandler> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Host != "export.arxiv.org")
            return await base.SendAsync(request, cancellationToken);

        string? key = request.Method == HttpMethod.Get
            ? "PaperFlow:arxiv:" + request.RequestUri.AbsoluteUri : null;

        if (key != null && cache.TryGetValue(key, out byte[]? hit) && hit is not null)
            return CachedResponse(request, hit);

        // Gate also prevents simultaneous duplicate GET requests.
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (key != null && cache.TryGetValue(key, out hit) && hit is not null)
                return CachedResponse(request, hit);

            var delay = nextAllowed - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                // Fail fast during long provider cooldown instead of queueing
                // HTTP requests for minutes and tying up web requests.
                if (delay > MinimumGap)
                    throw new HttpRequestException(
                        $"arXiv cooldown in progress ({Math.Ceiling(delay.TotalSeconds)} seconds).",
                        null, HttpStatusCode.TooManyRequests);
                await Task.Delay(delay, cancellationToken);
            }

            // Set BEFORE sending: even unsuccessful requests consume the slot.
            nextAllowed = DateTimeOffset.UtcNow + MinimumGap;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, timeout.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "arXiv request timed out");
                throw new HttpRequestException("arXiv request timed out.", ex,
                    HttpStatusCode.GatewayTimeout);
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            {
                var until = RetryDelay(response.Headers.RetryAfter);
                nextAllowed = DateTimeOffset.UtcNow + until;
                logger.LogWarning("arXiv returned {Status}. Cooldown {Seconds}s.",
                    (int)response.StatusCode, Math.Round(until.TotalSeconds));
                return response; // existing EnsureSuccessStatusCode throws, coordinator handles
            }

            if (key != null && response.IsSuccessStatusCode)
            {
                // Bounded cache for small Atom feeds; never cache errors.
                if ((response.Content.Headers.ContentLength ?? 0) <= 2_000_000)
                {
                    byte[] body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    if (body.Length <= 2_000_000)
                    {
                        cache.Set(key, body, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                            Size = null
                        });
                        response.Content.Dispose();
                        response.Content = new ByteArrayContent(body);
                        response.Content.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue("application/atom+xml");
                    }
                }
            }
            return response;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static HttpResponseMessage CachedResponse(HttpRequestMessage request, byte[] body) =>
        new(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new ByteArrayContent(body)
        };

    private static TimeSpan RetryDelay(System.Net.Http.Headers.RetryConditionHeaderValue? retry)
    {
        TimeSpan delay = retry?.Delta ??
            (retry?.Date is DateTimeOffset date
                ? date - DateTimeOffset.UtcNow
                : DefaultCooldown);
        if (delay < MinimumGap) delay = MinimumGap;
        if (delay > MaxCooldown) delay = MaxCooldown;
        return delay;
    }
}

}

