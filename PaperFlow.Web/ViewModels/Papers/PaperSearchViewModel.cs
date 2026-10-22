namespace PaperFlow.Web.ViewModels.Papers
{
    using System.ComponentModel.DataAnnotations;
    using PaperFlow.Services.Models;

    public class PaperSearchViewModel : IValidatableObject
    {
        public bool Advanced { get; set; } = true;

        [Display(Name = "Keywords")]
        [StringLength(200)]
        public string? Query { get; set; }
            = "mobile robot navigation";

        [Display(Name = "Search in")]
        public string SearchField { get; set; } = "all";

        [Display(Name = "Research Category")]
        public string? Category { get; set; } = "cs.RO";

        [Display(Name = "Author")]
        [StringLength(200)]
        public string? Author { get; set; }

        [Display(Name = "Submitted From")]
        [DataType(DataType.Date)]
        public DateOnly? SubmittedFrom { get; set; }
        = DateOnly.FromDateTime(DateTime.Today).AddDays(-30);

        [Display(Name = "Submitted To")]
        [DataType(DataType.Date)]
        public DateOnly? SubmittedTo { get; set; }
            = DateOnly.FromDateTime(DateTime.Today);


        [Display(Name = "Sort By")]
        public string SortBy { get; set; } = "submittedDate";

        [Display(Name = "Sort Order")]
        public string SortOrder { get; set; } = "descending";

        [Display(Name = "Maximum Results")]
        [Range(1, 50)]
        public int MaxResults { get; set; } = 20;

        [Display(Name = "Only With PDF")]
        public bool OnlyWithPdf { get; set; }

        public List<string> SelectedSources { get; set; }
            = new() { "arxiv" };

        public List<string> AvailableSources { get; set; }
            = new();

        // Identifies the cached search for the authenticated user.
        public string SearchId { get; set; } = string.Empty;

        // arXiv IDs already stored in the current user's SQL Server library.
        public HashSet<string> SavedExternalIds { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        // Paging state is maintained in the authenticated user's search cache.
        public int NextStart { get; set; }
        public bool HasMoreResults { get; set; }

        public bool HasSearched { get; set; }

        public string? ErrorMessage { get; set; }

        public List<string> FailedSources { get; set; }
            = new();

        public IReadOnlyList<PaperSearchResult> Results
        { get; set; } = new List<PaperSearchResult>();

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (!Advanced &&
                string.IsNullOrWhiteSpace(Query))
            {
                yield return new ValidationResult(
                    "Enter search keywords.",
                    new[] { nameof(Query) });
            }

            if (Advanced &&
                SubmittedFrom.HasValue &&
                SubmittedTo.HasValue &&
                SubmittedFrom > SubmittedTo)
            {
                yield return new ValidationResult(
                    "Start date must not be after end date.",
                    new[] { nameof(SubmittedTo) });
            }


            if (SelectedSources.Count == 0)
            {
                yield return new ValidationResult(
                    "Select at least one source.",
                    new[] { nameof(SelectedSources) });
            }
        }

        public PaperSearchCriteria ToCriteria()
        {
            return new PaperSearchCriteria
            {
                Keywords = Query?.Trim(),
                SearchField = Advanced ? SearchField : "all",
                Category = Advanced ? Category : null,
                Author = Advanced ? Author : null,
                SubmittedFrom = Advanced ? SubmittedFrom : DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-30),
                SubmittedTo = Advanced ? SubmittedTo : DateOnly.FromDateTime(DateTime.UtcNow.Date),
                SortBy = Advanced ? SortBy : "relevance",
                SortOrder = Advanced ? SortOrder : "descending",
                MaxResults = Advanced ? MaxResults : 10,
                OnlyWithPdf = Advanced && OnlyWithPdf
            };
        }
    }
}

