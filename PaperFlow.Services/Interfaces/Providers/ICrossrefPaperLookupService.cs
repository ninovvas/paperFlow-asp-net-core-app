using PaperFlow.Services.Models;

namespace PaperFlow.Services.Interfaces.Providers;

public interface ICrossrefPaperLookupService
{
    Task<PaperSearchResult?> GetByDoiAsync(string doi, CancellationToken cancellationToken = default);
}
