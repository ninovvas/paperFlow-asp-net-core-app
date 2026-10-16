namespace PaperFlow.Services.Interfaces
{
    using PaperFlow.Services.Models;
    using System;
    using System.Collections.Generic;
    using System.Text;
    public interface IPaperImportService
    {
        Task<PaperImportResult> ImportFromArxivAsync(
            string arxivId,
            string userId,
            CancellationToken cancellationToken = default);
    }
}
