namespace PaperFlow.Data.Models
{
    public class PaperCategory
    {
        public int PaperId { get; set; }

        public virtual Paper Paper { get; set; } = null!;

        public int CategoryId { get; set; }

        public virtual Category Category { get; set; } = null!;
    }
}
