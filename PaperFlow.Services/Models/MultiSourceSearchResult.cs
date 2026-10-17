
namespace PaperFlow.Services.Models
{
    public class MultiSourceSearchResult
    {
        public List<PaperSearchResult> Papers { get; set; }
            = new();

        public List<string> SuccessfulSources { get; set; }
            = new();

        public List<string> FailedSources { get; set; }
            = new();

        public int TotalResults => Papers.Count;
    }
}

