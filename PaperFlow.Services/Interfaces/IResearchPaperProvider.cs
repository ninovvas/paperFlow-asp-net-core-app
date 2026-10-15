using PaperFlow.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PaperFlow.Services.Interfaces
{
    public interface IResearchPaperProvider
    {
        string ProviderName { get; }

        Task<IReadOnlyList<PaperSearchResult>> SearchAsync(
            string query,
            int maxResults = 10,
            CancellationToken cancellationToken = default);
    }
}
