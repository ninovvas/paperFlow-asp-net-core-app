namespace PaperFlow.Web.ViewModels.Papers
{

    using System.ComponentModel.DataAnnotations;
    using PaperFlow.Services.Models;
    using PaperFlow.Common;

    public sealed class PaperReadingEditViewModel : IValidatableObject
    {
        public int PaperId { get; set; }

        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Reading Status")]
        public string Status { get; set; } = "NotStarted";

        [Display(Name = "Reading Deadline")]
        [DataType(DataType.Date)]
        public DateOnly? DueDate { get; set; }

        [Display(Name = "Personal Rating")]
        [Range(DataConstants.Paper.MinRating, DataConstants.Paper.MaxRating)]
        public int? Rating { get; set; }

        [Display(Name = "Current Page")]
        [Range(DataConstants.Paper.MinCurrentPage, DataConstants.Paper.MaxPages)]
        public int CurrentPage { get; set; }

        [Display(Name = "Total Pages")]
        [Range(DataConstants.Paper.MinPages, DataConstants.Paper.MaxPages)]
        public int? TotalPages { get; set; }

        [Display(Name = "Personal Notes")]
        [MaxLength(DataConstants.Paper.NotesMaxLength)]
        public string? Notes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TotalPages.HasValue &&
                CurrentPage > TotalPages.Value)
            {
                yield return new ValidationResult("Current Page cannot exceed Total Pages.", new[] { nameof(CurrentPage), nameof(TotalPages) });
            }

            if (!TotalPages.HasValue && CurrentPage > 0 && Status != "Completed")
            {
                yield return new ValidationResult(
                    "Enter Total Pages before setting Current Page.", new[] { nameof(TotalPages) });
            }

            string[] allowedStatuses =
            {
                "NotStarted",
                "WantToRead",
                "Reading",
                "Completed",
                "OnHold"
            };

            if (!allowedStatuses.Contains(Status))
            {
                yield return new ValidationResult("Select a valid reading status.", new[] { nameof(Status) });
            }
        }

        public PaperReadingDto ToDto()
        {
            return new PaperReadingDto
            {
                PaperId = PaperId,
                Status = Status,
                DueDate = DueDate,
                Rating = Rating,
                CurrentPage = CurrentPage,
                TotalPages = TotalPages,
                Notes = Notes
            };
        }

        public static PaperReadingEditViewModel FromDto(PaperReadingDto dto)
        {
            return new PaperReadingEditViewModel
            {
                PaperId = dto.PaperId,
                Title = dto.Title,
                Status = dto.Status,
                DueDate = dto.DueDate,
                Rating = dto.Rating,
                CurrentPage = dto.CurrentPage,
                TotalPages = dto.TotalPages,
                Notes = dto.Notes
            };
        }
    }
}

