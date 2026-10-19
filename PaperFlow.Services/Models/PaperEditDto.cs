
namespace PaperFlow.Services.Models
{
    public sealed class PaperEditDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Abstract { get; set; }

        public DateOnly? PublicationDate { get; set; }

        public string? Doi { get; set; }

        public string? PublicationUrl { get; set; }

        public string? PdfUrl { get; set; }

        public List<string> Authors { get; set; } = new();

        public List<string> Categories { get; set; } = new();
    }
}

