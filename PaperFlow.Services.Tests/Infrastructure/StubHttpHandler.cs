
namespace PaperFlow.Services.Tests.Infrastructure
{
    using System.Net;


public sealed class StubHttpHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
{
    public Uri? RequestedUri { get; private set; }
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUri = request.RequestUri;
        CallCount++;
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body)
        });
    }
}
}
