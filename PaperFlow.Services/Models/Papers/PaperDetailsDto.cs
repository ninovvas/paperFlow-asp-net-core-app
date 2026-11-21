
namespace PaperFlow.Services.Models
{
    public sealed class PaperDetailsDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Abstract { get; set; }

        public DateOnly? PublicationDate { get; set; }

        public string? Doi { get; set; }

        public string? ExternalId { get; set; }

        public string? PublicationUrl { get; set; }

        public string? PdfUrl { get; set; }

        public string? LocalPdfPath { get; set; }

        public string? ResearchSourceName { get; set; }

        public string ReadingStatus { get; set; } =
            string.Empty;

        public DateOnly? DueDate { get; set; }

        public DateTime? StartedReadingOn { get; set; }

        public DateTime? CompletedOn { get; set; }

        public int? Rating { get; set; }

        public int? TotalPages { get; set; }

        public int CurrentPage { get; set; }

        public int? ReadingProgressPercent =>
            TotalPages is > 0
                ? (int)Math.Clamp(
                    Math.Round(
                        100.0 * CurrentPage / TotalPages.Value),
                    0,
                    100)
                : null;

        public string? Notes { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public List<PaperAuthorDto> Authors { get; set; }
            = new();

        public List<string> Categories { get; set; }
            = new();
    }

    public sealed class PaperAuthorDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Orcid { get; set; }

        public int AuthorOrder { get; set; }
    }
}

