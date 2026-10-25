namespace PaperFlow.Data.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using static PaperFlow.Common.DataConstants.Category;
    public class Category
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(NameMaxLength)]
        public string Name { get; set; } = null!;

        [MaxLength(DescriptionMaxLength)]
        public string? Description { get; set; }

        public virtual ICollection<PaperCategory> PaperCategories{ get; set; } = new HashSet<PaperCategory>();
    }
}
