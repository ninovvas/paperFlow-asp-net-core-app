
namespace PaperFlow.Services.Providers
{
    using System.Net;
    public sealed class ArxivRequestCoordinator
    {
        private static readonly TimeSpan MinimumInterval =
            TimeSpan.FromSeconds(3);

        private static readonly TimeSpan DefaultCooldown =
            TimeSpan.FromSeconds(60);

        private static readonly TimeSpan MaximumCooldown =
            TimeSpan.FromMinutes(15);

        private readonly SemaphoreSlim semaphore = new(1, 1);

        private DateTimeOffset nextAllowedRequest =
            DateTimeOffset.MinValue;

        public async Task<HttpResponseMessage> SendAsync(
            Func<CancellationToken, Task<HttpResponseMessage>> send,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                TimeSpan waitTime =
                    nextAllowedRequest - DateTimeOffset.UtcNow;

                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(
                        waitTime,
                        cancellationToken);
                }

                // Set the minimum interval before sending.
                nextAllowedRequest =
                    DateTimeOffset.UtcNow + MinimumInterval;

                HttpResponseMessage response =
                    await send(cancellationToken);

                if (response.StatusCode ==
                    HttpStatusCode.TooManyRequests)
                {
                    TimeSpan cooldown =
                        GetCooldown(response);

                    DateTimeOffset cooldownUntil =
                        DateTimeOffset.UtcNow + cooldown;

                    if (cooldownUntil > nextAllowedRequest)
                    {
                        nextAllowedRequest = cooldownUntil;
                    }
                }

                return response;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static TimeSpan GetCooldown(
            HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;

            if (retryAfter?.Delta is TimeSpan delta)
            {
                return ClampCooldown(delta);
            }

            if (retryAfter?.Date is DateTimeOffset date)
            {
                return ClampCooldown(
                    date - DateTimeOffset.UtcNow);
            }

            return DefaultCooldown;
        }

        private static TimeSpan ClampCooldown(TimeSpan value)
        {
            if (value < MinimumInterval)
            {
                return MinimumInterval;
            }

            return value > MaximumCooldown
                ? MaximumCooldown
                : value;
        }
    }
}

