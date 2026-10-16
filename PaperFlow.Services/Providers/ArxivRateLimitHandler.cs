
namespace PaperFlow.Services.Providers
{
    public sealed class ArxivRateLimitHandler : DelegatingHandler
    {
        private readonly ArxivRequestCoordinator coordinator;

        public ArxivRateLimitHandler(
            ArxivRequestCoordinator coordinator)
        {
            this.coordinator = coordinator;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return coordinator.SendAsync(
                ct => base.SendAsync(request, ct),
                cancellationToken);
        }
    }
}

