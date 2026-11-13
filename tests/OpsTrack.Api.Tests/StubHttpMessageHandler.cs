using System.Collections.Concurrent;
using System.Net;

namespace OpsTrack.Api.Tests;

/// <summary>
/// Fake HTTP endpoint for outbound calls (the SLA notification webhook).
/// Records every request and answers with <see cref="ResponseStatus"/>.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<CapturedRequest> _requests = new();

    public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;

    public IReadOnlyList<CapturedRequest> Requests => _requests.ToArray();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        _requests.Enqueue(new CapturedRequest(request.Method, request.RequestUri, body));
        return new HttpResponseMessage(ResponseStatus);
    }
}

public sealed record CapturedRequest(HttpMethod Method, Uri Uri, string Body);
