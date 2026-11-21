
namespace PaperFlow.Services.Models
{
    public sealed class PaperReadingDto
    {
        public int PaperId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = "NotStarted";

        public DateOnly? DueDate { get; set; }

        public int? Rating { get; set; }

        public int CurrentPage { get; set; }

        public int? TotalPages { get; set; }

        public string? Notes { get; set; }

        public DateTime? StartedReadingOn { get; set; }

        public DateTime? CompletedOn { get; set; }
    }
}

