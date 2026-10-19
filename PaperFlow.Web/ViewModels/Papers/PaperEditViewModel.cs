namespace PaperFlow.Web.ViewModels.Papers
{

    using System.ComponentModel.DataAnnotations;

    using PaperFlow.Common;
    using PaperFlow.Services.Models;

    public sealed class PaperEditViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(
            DataConstants.Paper.TitleMaxLength,
            MinimumLength = DataConstants.Paper.TitleMinLength)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(DataConstants.Paper.AbstractMaxLength)]
        public string? Abstract { get; set; }

        [Display(Name = "Publication Date")]
        [DataType(DataType.Date)]
        public DateOnly? PublicationDate { get; set; }

        [StringLength(DataConstants.Paper.DoiMaxLength)]
        public string? Doi { get; set; }

        [Display(Name = "Publication URL")]
        [StringLength(DataConstants.Paper.UrlMaxLength)]
        public string? PublicationUrl { get; set; }

        [Display(Name = "PDF URL")]
        [StringLength(DataConstants.Paper.UrlMaxLength)]
        public string? PdfUrl { get; set; }

        [Display(Name = "Authors (comma-separated)")]
        public string? Authors { get; set; }

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

            foreach (string author in Split(Authors))
            {
                if (author.Length >
                    DataConstants.Author.NameMaxLength)
                {
                    yield return new ValidationResult("An author name exceeds the maximum length.", new[] { nameof(Authors) });
                }
            }

            foreach (string category in Split(Categories))
            {
                if (category.Length >
                    DataConstants.Category.NameMaxLength)
                {
                    yield return new ValidationResult("A category name exceeds the maximum length.", new[] { nameof(Categories) });
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

