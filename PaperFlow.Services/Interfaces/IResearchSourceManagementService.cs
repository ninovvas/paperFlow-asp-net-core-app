namespace PaperFlow.Services.Interfaces
{

    using PaperFlow.Services.Models;



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
