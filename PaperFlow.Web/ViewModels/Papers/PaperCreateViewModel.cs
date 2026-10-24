namespace PaperFlow.Web.ViewModels.Papers
{

    using System.ComponentModel.DataAnnotations;
    using PaperFlow.Common;
    public class PaperCreateViewModel
    {

        public List<string> SelectedSources { get; set; } = new() { "arxiv" };

        public List<string> AvailableSources { get; set; } = new();

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
        [MaxLength(DataConstants.Paper.DoiMaxLength)]
        [RegularExpression(DataConstants.Paper.DoiPattern,
            ErrorMessage = DataConstants.Paper.DoiErrorMessage)]
        public string? Doi { get; set; }

        [Display(Name = "Publication URL")]
        [Url]
        [MaxLength(DataConstants.Paper.UrlMaxLength)]
        public string? PublicationUrl { get; set; }

        [Display(Name = "PDF URL")]
        [Url]
        [MaxLength(DataConstants.Paper.UrlMaxLength)]
        public string? PdfUrl { get; set; }

        [CommaSeparatedList(DataConstants.Author.NameMaxLength)]
        [Display(Name = "Authors (comma-separated)")]
        public string? Authors { get; set; }

        [CommaSeparatedList(DataConstants.Category.NameMaxLength)]
        [Display(Name = "Categories (comma-separated)")]
        public string? Categories { get; set; }

    }
}
