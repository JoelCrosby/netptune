using System.Net;
using System.Text;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Web;

public sealed class WebContentFetcher : IWebContentFetcher
{
    public const string HttpClientName = "netptune.ai.web";

    private static readonly string[] ReadableContentTypes =
    [
        "text/html",
        "application/xhtml+xml",
        "text/plain",
        "text/markdown",
        "text/xml",
        "application/xml",
        "application/json",
    ];

    private readonly HttpClient Client;
    private readonly AiWebOptions Options;

    public WebContentFetcher(HttpClient client, IOptions<AiOptions> options)
    {
        Client = client;
        Options = options.Value.Web;
    }

    public async Task<WebFetchResult> Fetch(string url, CancellationToken cancellationToken)
    {
        var isAbsolute = Uri.TryCreate(url, UriKind.Absolute, out var uri);

        if (!isAbsolute)
        {
            return WebFetchResult.Failed($"{url} is not an absolute URL.");
        }

        var response = await Send(uri!, cancellationToken);

        if (response.Error is not null)
        {
            return WebFetchResult.Failed(response.Error);
        }

        using var message = response.Message!;

        if (!message.IsSuccessStatusCode)
        {
            return WebFetchResult.Failed($"{response.FinalUrl} returned {(int)message.StatusCode} {message.StatusCode}.");
        }

        var contentType = message.Content.Headers.ContentType?.MediaType;
        var isReadable = contentType is not null && ReadableContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);

        if (!isReadable)
        {
            return WebFetchResult.Failed($"{response.FinalUrl} is {contentType ?? "an unknown type"}, which cannot be read as text.");
        }

        var body = await ReadCapped(message, cancellationToken);
        var isHtml = contentType!.Contains("html", StringComparison.OrdinalIgnoreCase);

        if (!isHtml)
        {
            return new WebFetchResult
            {
                IsSuccess = true,
                FinalUrl = response.FinalUrl,
                ContentType = contentType,
                Content = Cap(body),
            };
        }

        var readable = await WebReadableText.Parse(body, cancellationToken);

        return new WebFetchResult
        {
            IsSuccess = true,
            FinalUrl = response.FinalUrl,
            Title = readable.Title,
            ContentType = contentType,
            Content = Cap(readable.Text),
        };
    }

    private sealed record FetchResponse
    {
        public HttpResponseMessage? Message { get; init; }

        public string? FinalUrl { get; init; }

        public string? Error { get; init; }
    }

    private async Task<FetchResponse> Send(Uri uri, CancellationToken cancellationToken)
    {
        var current = uri;

        for (var hop = 0; hop <= Options.MaxRedirects; hop++)
        {
            var verdict = await WebEgressGuard.CheckHost(current, cancellationToken);

            if (!verdict.IsAllowed)
            {
                return new FetchResponse { Error = verdict.Reason };
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, current);

            var message = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var location = ReadRedirect(message, current);

            if (location is null)
            {
                return new FetchResponse { Message = message, FinalUrl = current.ToString() };
            }

            message.Dispose();
            current = location;
        }

        return new FetchResponse { Error = $"{uri} redirected more than {Options.MaxRedirects} times." };
    }

    private static Uri? ReadRedirect(HttpResponseMessage message, Uri current)
    {
        var isRedirect = message.StatusCode is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

        if (!isRedirect)
        {
            return null;
        }

        var location = message.Headers.Location;

        if (location is null)
        {
            return null;
        }

        return location.IsAbsoluteUri ? location : new Uri(current, location);
    }

    private async Task<string> ReadCapped(HttpResponseMessage message, CancellationToken cancellationToken)
    {
        await using var stream = await message.Content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[8192];
        var written = new MemoryStream();

        while (written.Length < Options.MaxResponseBytes)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);

            if (read == 0)
            {
                break;
            }

            var allowed = (int)Math.Min(read, Options.MaxResponseBytes - written.Length);

            written.Write(buffer, 0, allowed);
        }

        return Encoding.UTF8.GetString(written.ToArray());
    }

    private string Cap(string content)
    {
        var isWithinLimit = content.Length <= Options.MaxDocumentCharacters;

        if (isWithinLimit)
        {
            return content;
        }

        return content[..Options.MaxDocumentCharacters];
    }
}
