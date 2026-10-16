
using Microsoft.Extensions.DependencyInjection;

using PaperFlow.Services.Interfaces;
using PaperFlow.Services.Providers;

namespace PaperFlow.Services.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPaperFlowServices(
            this IServiceCollection services)
        {
            // =========================================
            // Shared arXiv request coordination
            // =========================================

            services.AddSingleton<ArxivRequestCoordinator>();

            services.AddTransient<ArxivRateLimitHandler>();

            // =========================================
            // Search provider
            // =========================================

            services
                .AddHttpClient<
                    IResearchPaperProvider,
                    ArxivSearchService>(client =>
                    {
                        ConfigureArxivClient(client);
                    })
                .AddHttpMessageHandler<ArxivRateLimitHandler>();

            // =========================================
            // Single paper lookup
            // =========================================

            services
                .AddHttpClient<
                    IArxivPaperLookupService,
                    ArxivPaperLookupService>(client =>
                    {
                        ConfigureArxivClient(client);
                    })
                .AddHttpMessageHandler<ArxivRateLimitHandler>();

            // =========================================
            // Database import
            // =========================================

            services.AddScoped<
                IPaperImportService,
                PaperImportService>();

            return services;
        }

        private static void ConfigureArxivClient(
            HttpClient client)
        {
            client.Timeout = TimeSpan.FromSeconds(120);

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "PaperFlow/1.0");
        }
    }
}
