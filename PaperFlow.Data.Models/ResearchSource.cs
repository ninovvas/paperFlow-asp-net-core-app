namespace PaperFlow.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text;
    using static PaperFlow.Common.DataConstants.ResearchSource;
    public class ResearchSource
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(NameMaxLength)]
        public string Name { get; set; } = null!;

        [Url]
        [MaxLength(UrlMaxLength)]
        public string? WebsiteUrl { get; set; }

        public bool IsEnabled { get; set; } = true;

        public virtual ICollection<Paper> Papers { get; set; } = new HashSet<Paper>();
    }
}
