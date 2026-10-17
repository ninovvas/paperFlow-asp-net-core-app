

namespace PaperFlow.Web.ViewModels.Papers
{

    using System.ComponentModel.DataAnnotations;
    using PaperFlow.Common;
    public class PaperCreateViewModel
    {
        [Required]
        [StringLength(
            DataConstants.Paper.TitleMaxLength,
            MinimumLength = DataConstants.Paper.TitleMinLength)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(DataConstants.Paper.AbstractMaxLength)]
        public string? Abstract { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? PublicationDate { get; set; }

        [MaxLength(DataConstants.Paper.DoiMaxLength)]
        public string? Doi { get; set; }

        [Url]
        [MaxLength(DataConstants.Paper.UrlMaxLength)]
        public string? PublicationUrl { get; set; }

        [Url]
        [MaxLength(DataConstants.Paper.UrlMaxLength)]
        public string? PdfUrl { get; set; }

        [Display(Name = "Authors (comma-separated)")]
        public string? Authors { get; set; }

        [Display(Name = "Categories (comma-separated)")]
        public string? Categories { get; set; }
    }
}

