
using Microsoft.AspNetCore.Identity;
using PaperFlow.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PaperFlow.Common.DataConstants.Paper;

namespace PaperFlow.Data.Models
{
    public class Paper
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(
            TitleMaxLength,
            MinimumLength = TitleMinLength)]
        public string Title { get; set; } = null!;

        [MaxLength(AbstractMaxLength)]
        public string? Abstract { get; set; }

        public DateOnly? PublicationDate { get; set; }

        [MaxLength(DoiMaxLength)]
        public string? Doi { get; set; }

        [MaxLength(ExternalIdMaxLength)]
        public string? ExternalId { get; set; }

        [Url]
        [MaxLength(UrlMaxLength)]
        public string? PublicationUrl { get; set; }

        [Url]
        [MaxLength(UrlMaxLength)]
        public string? PdfUrl { get; set; }

        [MaxLength(FilePathMaxLength)]
        public string? LocalPdfPath { get; set; }

        public int? ResearchSourceId { get; set; }

        [ForeignKey(nameof(ResearchSourceId))]
        public virtual ResearchSource? ResearchSource
        { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public virtual IdentityUser User
        { get; set; } = null!;

        public ReadingStatus Status { get; set; }
            = ReadingStatus.NotStarted;

        public DateOnly? DueDate { get; set; }

        public DateTime? StartedReadingOn { get; set; }

        public DateTime? CompletedOn { get; set; }

        [Range(MinRating, MaxRating)]
        public int? Rating { get; set; }

        [Range(MinPages, MaxPages)]
        public int? TotalPages { get; set; }

        [Range(0, MaxPages)]
        public int CurrentPage { get; set; } = 0;

        [MaxLength(NotesMaxLength)]
        public string? Notes { get; set; }

        public DateTime CreatedOn { get; set; }
            = DateTime.UtcNow;

        public DateTime? ModifiedOn { get; set; }

        public virtual ICollection<PaperAuthor> PaperAuthors{ get; set; } = new HashSet<PaperAuthor>();

        public virtual ICollection<PaperCategory> PaperCategories { get; set; } = new HashSet<PaperCategory>();
    }
}

