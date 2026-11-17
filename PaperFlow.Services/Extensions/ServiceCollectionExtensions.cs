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
            services.AddHttpClient<ArxivSearchService>(ConfigureArxivClient)
                .AddHttpMessageHandler<ArxivRateLimitHandler>();

            // Crossref metadata search (independent of arXiv rate limiting)
            services.AddHttpClient<CrossrefSearchService>(client =>
            {
                client.BaseAddress = new Uri("https://api.crossref.org/");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PaperFlow/1.0 (research application)");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            });

            services.AddHttpClient<ICrossrefPaperLookupService, CrossrefPaperLookupService>(client =>
            {
                client.BaseAddress = new Uri("https://api.crossref.org/");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PaperFlow/1.0 (research application)");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            });

            // OpenAlex search, DOI abstract lookup, and work import.
            services.AddMemoryCache();
            services.AddHttpClient<OpenAlexSearchService>(ConfigureOpenAlexClient);

            // Each provider has its own named HttpClient configuration.
            // Register the shared interface separately for provider discovery.
            services.AddTransient<IResearchPaperProvider>(sp => sp.GetRequiredService<ArxivSearchService>());
            services.AddTransient<IResearchPaperProvider>(sp => sp.GetRequiredService<CrossrefSearchService>());
            services.AddTransient<IResearchPaperProvider>(sp => sp.GetRequiredService<OpenAlexSearchService>());
            services.AddHttpClient<IOpenAlexWorkLookupService, OpenAlexWorkLookupService>(ConfigureOpenAlexClient);
            services.AddHttpClient<IOpenAlexAbstractService, OpenAlexAbstractService>(ConfigureOpenAlexClient);

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

            services.AddScoped<IPaperImportService, PaperImportService>();

            services.AddScoped<IPaperDetailsService, PaperDetailsService>();

            services.AddScoped<IPaperReadingService, PaperReadingService>();

            services.AddScoped<IPaperEditService, PaperEditService>();

            services.AddScoped<IPaperDeleteService, PaperDeleteService>();

            services.AddScoped<IPaperSavedStatusService, PaperSavedStatusService>();

            services.AddScoped<IStatisticsService, StatisticsService>();

            return services;
        }

        private static void ConfigureOpenAlexClient(HttpClient client)
        {
            client.BaseAddress = new Uri("https://api.openalex.org/");
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PaperFlow/1.0 (research application)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            // Optional key: configure via OpenAlex__ApiKey environment variable.
            string? key = Environment.GetEnvironmentVariable("OpenAlex__ApiKey");
            if (!string.IsNullOrWhiteSpace(key))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key.Trim());
        }

        private static void ConfigureArxivClient(
            HttpClient client)
        {
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PaperFlow/1.0");
        }
    }
}
