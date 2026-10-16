

namespace PaperFlow.Services.Interfaces
{
    using PaperFlow.Services.Models;
    using System;
    using System.Collections.Generic;
    using System.Text;
    public interface IArxivPaperLookupService
    {
        Task<PaperSearchResult?> GetByIdAsync(
            string arxivId,
            CancellationToken cancellationToken = default);
    }
}
