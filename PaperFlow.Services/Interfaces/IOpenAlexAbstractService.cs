namespace PaperFlow.Services.Interfaces
{



    public interface IOpenAlexAbstractService
    {
        Task<string?> GetAbstractByDoiAsync(string doi,
            CancellationToken cancellationToken = default);
    }
}


