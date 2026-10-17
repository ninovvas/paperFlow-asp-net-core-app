
namespace PaperFlow.Services.Models
{
    public class PaperSearchCriteria
    {
        public string? Keywords { get; set; }

        public string SearchField { get; set; } = "all";

        public string? Category { get; set; }

        public string? Author { get; set; }

        public DateOnly? SubmittedFrom { get; set; }

        public DateOnly? SubmittedTo { get; set; }

        public string SortBy { get; set; } = "relevance";

        public string SortOrder { get; set; } = "descending";

        public int Start { get; set; } = 0;

        public int MaxResults { get; set; } = 20;

        public bool OnlyWithPdf { get; set; }
    }
}

