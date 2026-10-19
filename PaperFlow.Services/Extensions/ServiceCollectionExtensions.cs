namespace PaperFlow.Services.Extensions
{

    using Microsoft.Extensions.DependencyInjection;

    using PaperFlow.Services.Interfaces;
    using PaperFlow.Services.Providers;
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPaperFlowServices(
            this IServiceCollection services)
        {
            // Shared arXiv rate limiting
            services.AddSingleton<ArxivRequestCoordinator>();
            services.AddTransient<ArxivRateLimitHandler>();

            // Research search providers
            services.AddHttpClient<
                IResearchPaperProvider,
                ArxivSearchService>(client =>
                {
                    ConfigureArxivClient(client);
                })
            .AddHttpMessageHandler<ArxivRateLimitHandler>();

            // Central multi-source coordinator
            services.AddScoped<IPaperSearchService, PaperSearchService>();

            // arXiv lookup for imports
            services.AddHttpClient<
                IArxivPaperLookupService,
                ArxivPaperLookupService>(client =>
                {
                    ConfigureArxivClient(client);
                })
            .AddHttpMessageHandler<ArxivRateLimitHandler>();

            services.AddScoped<IPaperImportService,PaperImportService>();

            services.AddScoped<IPaperDetailsService,PaperDetailsService>();

            services.AddScoped<IPaperReadingService, PaperReadingService>();

            services.AddScoped<IPaperEditService, PaperEditService>();

            services.AddScoped<IPaperDeleteService, PaperDeleteService>();
            return services;
        }

        private static void ConfigureArxivClient(
            HttpClient client)
        {
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PaperFlow/1.0");
        }
    }
}
