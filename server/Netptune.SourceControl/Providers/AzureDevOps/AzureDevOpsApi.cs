using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Core.SourceControl;
using Netptune.SourceControl.Providers.AzureDevOps.Dtos;

using Polly.Timeout;

namespace Netptune.SourceControl.Providers.AzureDevOps;

internal sealed record AzureDevOpsRequest
{
    public required string Organisation { get; init; }

    public required string Path { get; init; }

    public required string Token { get; init; }

    // A record prints every property, and the token must never reach a log.
    public override string ToString()
    {
        return $"{nameof(AzureDevOpsRequest)} {{ {nameof(Organisation)} = {Organisation}, {nameof(Path)} = {Path} }}";
    }
}

internal enum AzureDevOpsResponseKind
{
    Success = 0,
    Unauthorised = 1,
    Forbidden = 2,
    NotFound = 3,
}

internal sealed record AzureDevOpsResponse<T>
{
    public AzureDevOpsResponseKind Kind { get; init; }

    public HttpStatusCode StatusCode { get; init; }

    public T? Payload { get; init; }

    public bool IsSuccess => Kind == AzureDevOpsResponseKind.Success;
}

internal sealed class AzureDevOpsApi
{
    public const string ClientName = "netptune-source-control-azure-devops";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory ClientFactory;
    private readonly AzureDevOpsOptions Options;
    private readonly TimeProvider Clock;

    public AzureDevOpsApi(IHttpClientFactory clientFactory, IOptions<AzureDevOpsOptions> options, TimeProvider clock)
    {
        ClientFactory = clientFactory;
        Options = options.Value;
        Clock = clock;
    }

    public async Task<AzureDevOpsResponse<T>> Get<T>(AzureDevOpsRequest request, CancellationToken cancellationToken)
    {
        using var message = CreateMessage(request);

        var client = ClientFactory.CreateClient(ClientName);

        using var response = await SendOrThrowProviderException(client, message, cancellationToken);

        ThrowIfRateLimited(response);

        var failureKind = ReadFailureKind(response);

        if (failureKind is { } kind)
        {
            return new AzureDevOpsResponse<T> { Kind = kind, StatusCode = response.StatusCode };
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ReadErrorMessage(response, cancellationToken);

            throw new ScmProviderException(SourceControlProvider.AzureDevOps, (int)response.StatusCode, errorMessage);
        }

        var payload = await ReadPayload<T>(response, cancellationToken);

        if (payload is null)
        {
            return new AzureDevOpsResponse<T>
            {
                Kind = AzureDevOpsResponseKind.Unauthorised,
                StatusCode = response.StatusCode,
            };
        }

        return new AzureDevOpsResponse<T>
        {
            Kind = AzureDevOpsResponseKind.Success,
            StatusCode = response.StatusCode,
            Payload = payload,
        };
    }

    // Azure DevOps answers a credential it doesn't accept with its sign-in page, sometimes as a 203
    // and sometimes as a 200 reached through a redirect, so anything that isn't JSON is a rejection.
    private static AzureDevOpsResponseKind? ReadFailureKind(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var isJson = mediaType is not null && mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
        var isSignInPage = response.StatusCode == HttpStatusCode.NonAuthoritativeInformation
            || (response.IsSuccessStatusCode && !isJson);

        if (isSignInPage)
        {
            return AzureDevOpsResponseKind.Unauthorised;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => AzureDevOpsResponseKind.Unauthorised,
            HttpStatusCode.Forbidden => AzureDevOpsResponseKind.Forbidden,
            HttpStatusCode.NotFound => AzureDevOpsResponseKind.NotFound,
            _ => null,
        };
    }

    private static async Task<T?> ReadPayload<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);

            return payload;
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static async Task<HttpResponseMessage> SendOrThrowProviderException(
        HttpClient client,
        HttpRequestMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.SendAsync(message, cancellationToken);

            return response;
        }
        catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
        {
            throw new ScmProviderException(
                SourceControlProvider.AzureDevOps,
                (int)HttpStatusCode.ServiceUnavailable,
                exception.Message);
        }
    }

    private static bool IsTransportFailure(Exception exception, CancellationToken cancellationToken)
    {
        var isTimeout = exception is TimeoutRejectedException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

        return isTimeout || exception is HttpRequestException;
    }

    private HttpRequestMessage CreateMessage(AzureDevOpsRequest request)
    {
        var apiBaseUrl = Options.ApiBaseUrl.TrimEnd('/');
        var organisation = Uri.EscapeDataString(request.Organisation);
        var url = new Uri($"{apiBaseUrl}/{organisation}/{request.Path}");
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        var credential = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{request.Token}"));

        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.UserAgent.Add(new ProductInfoHeaderValue("Netptune", "1.0"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credential);

        return message;
    }

    private void ThrowIfRateLimited(HttpResponseMessage response)
    {
        var isTooManyRequests = response.StatusCode == HttpStatusCode.TooManyRequests;
        var retryAfter = response.Headers.RetryAfter;
        var isThrottled = isTooManyRequests || retryAfter is not null;

        if (!isThrottled)
        {
            return;
        }

        var retryAt = ReadRetryAt(retryAfter);

        throw new ScmRateLimitedException(SourceControlProvider.AzureDevOps, retryAt);
    }

    private DateTimeOffset ReadRetryAt(RetryConditionHeaderValue? retryAfter)
    {
        var now = Clock.GetUtcNow();

        if (retryAfter?.Delta is { } delay)
        {
            return now + delay;
        }

        return retryAfter?.Date ?? now.AddMinutes(1);
    }

    private static async Task<string?> ReadErrorMessage(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<AzureDevOpsErrorDto>(JsonOptions, cancellationToken);

            return error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
