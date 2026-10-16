using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Core.SourceControl;
using Netptune.SourceControl.Providers.GitHub.Dtos;

namespace Netptune.SourceControl.Providers.GitHub;

internal sealed record GitHubRequest
{
    public required HttpMethod Method { get; init; }

    public required string Url { get; init; }

    public string? BearerToken { get; init; }

    public object? Body { get; init; }

    public string Accept { get; init; } = "application/vnd.github+json";
}

internal sealed record GitHubResponse<T>
{
    public HttpStatusCode StatusCode { get; init; }

    public T? Payload { get; init; }

    public string? NextPageUrl { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

internal sealed class GitHubApi
{
    public const string ClientName = "netptune-source-control-github";

    private const string ApiVersion = "2022-11-28";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IHttpClientFactory ClientFactory;
    private readonly GitHubAppOptions Options;
    private readonly TimeProvider Clock;

    public GitHubApi(IHttpClientFactory clientFactory, IOptions<GitHubAppOptions> options, TimeProvider clock)
    {
        ClientFactory = clientFactory;
        Options = options.Value;
        Clock = clock;
    }

    public string WebBaseUrl => Options.WebBaseUrl;

    public async Task<GitHubResponse<T>> Send<T>(GitHubRequest request, CancellationToken cancellationToken)
    {
        using var message = CreateMessage(request);

        var client = ClientFactory.CreateClient(ClientName);

        using var response = await client.SendAsync(message, cancellationToken);

        ThrowIfRateLimited(response);

        var isExpectedFailure = response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity;

        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            var nextPageUrl = ReadNextPageUrl(response);

            return new GitHubResponse<T>
            {
                StatusCode = response.StatusCode,
                Payload = payload,
                NextPageUrl = nextPageUrl,
            };
        }

        var errorMessage = await ReadErrorMessage(response, cancellationToken);

        if (!isExpectedFailure)
        {
            throw new ScmProviderException(SourceControlProvider.GitHub, (int)response.StatusCode, errorMessage);
        }

        return new GitHubResponse<T>
        {
            StatusCode = response.StatusCode,
            ErrorMessage = errorMessage,
        };
    }

    private HttpRequestMessage CreateMessage(GitHubRequest request)
    {
        var url = ResolveUrl(request.Url);
        var message = new HttpRequestMessage(request.Method, url);

        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(request.Accept));
        message.Headers.Add("X-GitHub-Api-Version", ApiVersion);
        message.Headers.UserAgent.Add(new ProductInfoHeaderValue("Netptune", "1.0"));

        if (request.BearerToken is not null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.BearerToken);
        }

        if (request.Body is not null)
        {
            message.Content = JsonContent.Create(request.Body, request.Body.GetType(), options: JsonOptions);
        }

        return message;
    }

    private Uri ResolveUrl(string url)
    {
        // Checked by prefix because on Unix Uri.TryCreate reads "/repos/..." as an absolute file:// URI.
        var isApiPath = url.StartsWith('/');

        if (!isApiPath)
        {
            return new Uri(url, UriKind.Absolute);
        }

        var apiBaseUrl = Options.ApiBaseUrl.TrimEnd('/');

        return new Uri($"{apiBaseUrl}{url}");
    }

    private void ThrowIfRateLimited(HttpResponseMessage response)
    {
        var isThrottlingStatus = response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests;

        if (!isThrottlingStatus)
        {
            return;
        }

        var remaining = ReadHeader(response, "x-ratelimit-remaining");
        var isPrimaryLimitExhausted = remaining == "0";
        var retryAfter = response.Headers.RetryAfter?.Delta;
        var isSecondaryLimit = retryAfter is not null;

        if (!isPrimaryLimitExhausted && !isSecondaryLimit)
        {
            return;
        }

        var retryAt = isSecondaryLimit ? Clock.GetUtcNow() + retryAfter!.Value : ReadRateLimitReset(response);

        throw new ScmRateLimitedException(SourceControlProvider.GitHub, retryAt);
    }

    private DateTimeOffset ReadRateLimitReset(HttpResponseMessage response)
    {
        var reset = ReadHeader(response, "x-ratelimit-reset");
        var hasReset = long.TryParse(reset, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resetSeconds);

        if (!hasReset)
        {
            return Clock.GetUtcNow().AddMinutes(1);
        }

        return DateTimeOffset.FromUnixTimeSeconds(resetSeconds);
    }

    // Link headers come from GitHub, but the bearer token is attached to whatever they point at, so
    // only follow pages on the configured API host.
    private string? ReadNextPageUrl(HttpResponseMessage response)
    {
        var hasLinks = response.Headers.TryGetValues("Link", out var values);

        if (!hasLinks)
        {
            return null;
        }

        var nextUrl = values!
            .SelectMany(value => value.Split(','))
            .Select(ParseLink)
            .Where(link => link.Rel == "next")
            .Select(link => link.Url)
            .FirstOrDefault();

        if (nextUrl is null)
        {
            return null;
        }

        var isUri = Uri.TryCreate(nextUrl, UriKind.Absolute, out var next);

        if (!isUri)
        {
            return null;
        }

        var apiBase = new Uri(Options.ApiBaseUrl);
        var isSameOrigin = IsSameOrigin(next!, apiBase);

        return isSameOrigin ? nextUrl : null;
    }

    private static bool IsSameOrigin(Uri left, Uri right)
    {
        var comparison = Uri.Compare(
            left,
            right,
            UriComponents.SchemeAndServer,
            UriFormat.Unescaped,
            StringComparison.OrdinalIgnoreCase);

        return comparison == 0;
    }

    private static (string? Url, string? Rel) ParseLink(string link)
    {
        var parts = link.Split(';', StringSplitOptions.TrimEntries);
        var target = parts[0];
        var isBracketed = target.StartsWith('<') && target.EndsWith('>');

        if (!isBracketed)
        {
            return (null, null);
        }

        var url = target[1..^1];

        var rel = parts
            .Skip(1)
            .Where(part => part.StartsWith("rel=", StringComparison.OrdinalIgnoreCase))
            .Select(part => part[4..].Trim('"'))
            .FirstOrDefault();

        return (url, rel);
    }

    private static string? ReadHeader(HttpResponseMessage response, string name)
    {
        var found = response.Headers.TryGetValues(name, out var values);

        return found ? values!.FirstOrDefault() : null;
    }

    private static async Task<string?> ReadErrorMessage(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<GitHubErrorDto>(JsonOptions, cancellationToken);

            return error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
