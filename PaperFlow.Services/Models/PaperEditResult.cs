
namespace PaperFlow.Services.Models
{
    public enum PaperEditStatus
    {
        Success,
        NotFound,
        DuplicateDoi,
        ValidationFailed,
        Conflict
    }

    public sealed record PaperEditResult(PaperEditStatus Status, string Message);
}

