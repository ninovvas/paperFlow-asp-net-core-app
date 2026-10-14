namespace PaperFlow.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text;
    using static PaperFlow.Common.DataConstants.Author;
    public class Author
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(NameMaxLength)]
        public string FullName { get; set; } = null!;

        [MaxLength(OrcidMaxLength)]
        public string? Orcid { get; set; }

        public virtual ICollection<PaperAuthor> PaperAuthors
        { get; set; } = new HashSet<PaperAuthor>();
    }
}
