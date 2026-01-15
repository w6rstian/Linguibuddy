using System.Net;

namespace Linguibuddy.Tests.FakeHelpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    public HttpResponseMessage? Response { get; set; }
    public List<HttpRequestMessage> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(Response ?? new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}