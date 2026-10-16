

namespace PaperFlow.Services.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    public class PaperImportResult
    {
        public bool Success { get; init; }

        public bool AlreadyExists { get; init; }

        public int? PaperId { get; init; }

        public string Message { get; init; } = string.Empty;

        public static PaperImportResult Imported(int id)
        {
            return new PaperImportResult
            {
                Success = true,
                PaperId = id,
                Message = "Paper imported successfully."
            };
        }

        public static PaperImportResult Duplicate(int id)
        {
            return new PaperImportResult
            {
                AlreadyExists = true,
                PaperId = id,
                Message = "Paper already exists in your library."
            };
        }

        public static PaperImportResult NotFound()
        {
            return new PaperImportResult
            {
                Message = "Paper was not found on arXiv."
            };
        }
    }
}
