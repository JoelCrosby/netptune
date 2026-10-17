using System.Net;
using System.Text;

namespace Netptune.UnitTests.Netptune.SourceControl.Support;

public sealed record RecordedRequest
{
    public required HttpMethod Method { get; init; }

    public required Uri Uri { get; init; }

    public string? Authorization { get; init; }

    public string? Accept { get; init; }

    public string? Body { get; init; }
}

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private sealed record Route(HttpMethod Method, string Url, Func<HttpResponseMessage> Respond);

    private readonly List<Route> Routes = [];

    public List<RecordedRequest> Requests { get; } = [];

    public StubHttpMessageHandler On(
        HttpMethod method,
        string url,
        HttpStatusCode status,
        string? json = null,
        Action<HttpResponseMessage>? configure = null)
    {
        Routes.Add(new Route(method, url, () =>
        {
            var response = new HttpResponseMessage(status);

            if (json is not null)
            {
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            configure?.Invoke(response);

            return response;
        }));

        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new RecordedRequest
        {
            Method = request.Method,
            Uri = request.RequestUri!,
            Authorization = request.Headers.Authorization?.ToString(),
            Accept = request.Headers.Accept.ToString(),
            Body = body,
        });

        var route = Routes.LastOrDefault(candidate => Matches(candidate, request));

        if (route is null)
        {
            throw new InvalidOperationException($"No stubbed response for {request.Method} {request.RequestUri}.");
        }

        return route.Respond();
    }

    private static bool Matches(Route route, HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        var isSameMethod = route.Method == request.Method;
        var isAbsoluteRoute = route.Url.StartsWith("http", StringComparison.Ordinal);
        var target = isAbsoluteRoute ? uri.AbsoluteUri : uri.PathAndQuery;

        return isSameMethod && target == route.Url;
    }
}
