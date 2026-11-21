namespace PaperFlow.Services.Models.Search
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    public class PaperSearchResult
    {
        public string ExternalId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Abstract { get; set; }

        public DateOnly? PublicationDate { get; set; }

        public string? Doi { get; set; }

        public string? PublicationUrl { get; set; }

        public string? PdfUrl { get; set; }

        public string SourceName { get; set; } = string.Empty;

        public string? AbstractSource { get; set; }
        public string? Publisher { get; set; }
        public string? PublicationVenue { get; set; }
        public string? PublicationType { get; set; }
        public int? CitationCount { get; set; }

        public List<string> Authors { get; set; } = new();

        public List<string> Categories { get; set; } = new();
    }
}
