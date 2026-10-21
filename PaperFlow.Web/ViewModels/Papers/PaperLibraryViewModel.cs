using PaperFlow.Data.Models;

namespace PaperFlow.Web.ViewModels.Papers;

public sealed class PaperLibraryQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int? SourceId { get; set; }
    public string Sort { get; set; } = "added_desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public void Normalize()
    {
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        if (Search?.Length > 200) Search = Search[..200];
        Status = string.IsNullOrWhiteSpace(Status) ? null : Status.Trim();
        Page = Math.Max(1, Page);
        PageSize = 10;
        if (Sort is not ("added_desc" or "added_asc" or
            "title_asc" or "title_desc" or
            "published_asc" or "published_desc" or
            "status_asc" or "status_desc" or
            "rating_asc" or "rating_desc"))
            Sort = "added_desc";
    }
}

public sealed class PaperLibrarySourceOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class PaperLibraryViewModel
{
    public IReadOnlyList<Paper> Papers { get; set; } = Array.Empty<Paper>();
    public PaperLibraryQuery Query { get; set; } = new();
    public int TotalPapers { get; set; }
    public int ReadingCount { get; set; }
    public int CompletedCount { get; set; }
    public int FilteredCount { get; set; }
    public int TotalPages { get; set; } = 1;
    public IReadOnlyList<PaperLibrarySourceOption> Sources { get; set; }
        = Array.Empty<PaperLibrarySourceOption>();
}

