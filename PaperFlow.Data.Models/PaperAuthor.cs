namespace PaperFlow.Data.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    public class PaperAuthor
    {
        public int PaperId { get; set; }

        public virtual Paper Paper { get; set; } = null!;

        public int AuthorId { get; set; }

        public virtual Author Author
        { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int AuthorOrder { get; set; }
    }
}
