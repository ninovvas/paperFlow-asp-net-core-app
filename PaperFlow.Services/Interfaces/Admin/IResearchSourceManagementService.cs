namespace PaperFlow.Services.Interfaces.Admin
{
    using PaperFlow.Services.Models.Admin;

    public interface IResearchSourceManagementService
    {
        Task<IReadOnlyList<AdminResearchSourceDto>> GetSourcesAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetEnabledProviderIdsAsync(
            CancellationToken cancellationToken = default);

        Task<bool> SetEnabledAsync(string providerId, bool isEnabled,
            CancellationToken cancellationToken = default);
    }
}
