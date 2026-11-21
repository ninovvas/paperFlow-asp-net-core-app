namespace PaperFlow.Services.Models.Admin
{

    public sealed class AdminResearchSourceDto
    {
        public string ProviderId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string WebsiteUrl { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
        public int SavedPaperCount { get; init; }
    }
}
