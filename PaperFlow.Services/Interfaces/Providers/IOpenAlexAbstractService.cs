namespace PaperFlow.Services.Interfaces.Providers
{



    public interface IOpenAlexAbstractService
    {
        Task<string?> GetAbstractByDoiAsync(string doi,
            CancellationToken cancellationToken = default);
    }
}


