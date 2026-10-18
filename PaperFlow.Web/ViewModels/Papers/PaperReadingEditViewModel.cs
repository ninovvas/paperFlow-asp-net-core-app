namespace PaperFlow.Web.ViewModels.Papers
{

    using System.ComponentModel.DataAnnotations;
    using PaperFlow.Services.Models;

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
        [Range(1, 5)]
        public int? Rating { get; set; }

        [Display(Name = "Current Page")]
        [Range(0, 100000)]
        public int CurrentPage { get; set; }

        [Display(Name = "Total Pages")]
        [Range(1, 100000)]
        public int? TotalPages { get; set; }

        [Display(Name = "Personal Notes")]
        [MaxLength(10000)]
        public string? Notes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TotalPages.HasValue &&
                CurrentPage > TotalPages.Value)
            {
                yield return new ValidationResult("Current Page cannot exceed Total Pages.",new[] {nameof(CurrentPage), nameof(TotalPages)});
            }

            if (!TotalPages.HasValue && CurrentPage > 0)
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

