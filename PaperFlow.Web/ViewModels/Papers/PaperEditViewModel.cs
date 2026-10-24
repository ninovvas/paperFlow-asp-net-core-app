namespace PaperFlow.Web.ViewModels.Papers
{

    using System.ComponentModel.DataAnnotations;

    using PaperFlow.Common;
    using PaperFlow.Services.Models;

    public sealed class PaperEditViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Display(Name = "Paper Title")]
        [Required]
        [StringLength(
            DataConstants.Paper.TitleMaxLength,
            MinimumLength = DataConstants.Paper.TitleMinLength)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Abstract")]
        [MaxLength(DataConstants.Paper.AbstractMaxLength)]
        public string? Abstract { get; set; }

        [Display(Name = "Publication Date")]
        [DataType(DataType.Date)]
        public DateOnly? PublicationDate { get; set; }

        [Display(Name = "DOI")]
        [StringLength(DataConstants.Paper.DoiMaxLength)]
        [RegularExpression(DataConstants.Paper.DoiPattern,
            ErrorMessage = DataConstants.Paper.DoiErrorMessage)]
        public string? Doi { get; set; }

        [Display(Name = "Publication URL")]
        [StringLength(DataConstants.Paper.UrlMaxLength)]
        public string? PublicationUrl { get; set; }

        [Display(Name = "PDF URL")]
        [StringLength(DataConstants.Paper.UrlMaxLength)]
        public string? PdfUrl { get; set; }

        [CommaSeparatedList(DataConstants.Author.NameMaxLength)]
        [Display(Name = "Authors (comma-separated)")]
        public string? Authors { get; set; }

        [CommaSeparatedList(DataConstants.Category.NameMaxLength)]
        [Display(Name = "Categories (comma-separated)")]
        public string? Categories { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            foreach (var item in new[]
            {
                (nameof(PublicationUrl), PublicationUrl),
                (nameof(PdfUrl), PdfUrl)
            })
            {
                if (string.IsNullOrWhiteSpace(item.Item2))
                {
                    continue;
                }

                if (!Uri.TryCreate(
                        item.Item2,
                        UriKind.Absolute,
                        out Uri? uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp &&
                     uri.Scheme != Uri.UriSchemeHttps))
                {
                    yield return new ValidationResult(
                        "Enter a valid HTTP or HTTPS URL.",
                        new[] { item.Item1 });
                }
            }

        }

        public PaperEditDto ToDto()
        {
            return new PaperEditDto
            {
                Id = Id,
                Title = Title,
                Abstract = Abstract,
                PublicationDate = PublicationDate,
                Doi = Doi,
                PublicationUrl = PublicationUrl,
                PdfUrl = PdfUrl,
                Authors = Split(Authors),
                Categories = Split(Categories)
            };
        }

        public static PaperEditViewModel FromDto(PaperEditDto dto)
        {
            return new PaperEditViewModel
            {
                Id = dto.Id,
                Title = dto.Title,
                Abstract = dto.Abstract,
                PublicationDate = dto.PublicationDate,
                Doi = dto.Doi,
                PublicationUrl = dto.PublicationUrl,
                PdfUrl = dto.PdfUrl,
                Authors = string.Join(", ", dto.Authors),
                Categories = string.Join(", ", dto.Categories)
            };
        }

        private static List<string> Split(string? text)
        {
            return (text ?? string.Empty)
                .Split(
                    ',',
                    StringSplitOptions.TrimEntries |
                    StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}

