using System.ClientModel;
using System.Text.Json;

using Anthropic.Exceptions;

namespace Netptune.Ai.Providers;

public static class AiProviderErrors
{
    private const int MaxReportedLength = 240;

    private const string InvalidKey =
        "The provider rejected the API key. Check the key in your assistant settings.";

    private const string Forbidden =
        "The API key is not allowed to use this model. Pick another model or use a key with access to it.";

    private const string OutOfCredit =
        "The provider account is out of credit. Add credit to the account that owns this API key, then try again.";

    private const string RateLimited =
        "The provider is rate limiting this key. Try again shortly.";

    private const string ModelMissing =
        "The provider does not know the selected model. Pick a different model in the assistant settings.";

    private const string TooLarge =
        "The conversation is too large for this model. Start a new chat and try again.";

    private const string Rejected =
        "The provider rejected the request.";

    private const string Unavailable =
        "The provider is unavailable right now. Try again shortly.";

    private static readonly string[] CreditMarkers =
    [
        "credit balance",
        "insufficient_quota",
        "exceeded your current quota",
        "purchase credits",
        "billing details",
    ];

    public static string? Describe(Exception exception)
    {
        var failure = ReadFailure(exception);
        var isOutOfCredit = failure is not null && IsOutOfCredit(failure);

        if (isOutOfCredit)
        {
            return OutOfCredit;
        }

        var described = exception switch
        {
            AnthropicUnauthorizedException => InvalidKey,
            AnthropicForbiddenException => Forbidden,
            AnthropicRateLimitException => RateLimited,
            AnthropicNotFoundException => ModelMissing,
            AnthropicUnprocessableEntityException => TooLarge,
            AnthropicApiException api => DescribeStatus((int)api.StatusCode),
            AnthropicServiceException => Unavailable,
            ClientResultException result => DescribeStatus(result.Status),
            _ => null,
        };

        if (described is not null)
        {
            return described;
        }

        if (failure is null)
        {
            return null;
        }

        var reported = ReadReportedMessage(failure.Body);

        return reported is null ? Rejected : $"{Rejected} {reported}";
    }

    private sealed record ProviderFailure(int Status, string? Body);

    private static ProviderFailure? ReadFailure(Exception exception)
    {
        return exception switch
        {
            AnthropicApiException api => new ProviderFailure((int)api.StatusCode, api.ResponseBody),
            ClientResultException result => new ProviderFailure(result.Status, result.Message),
            _ => null,
        };
    }

    // A spent account is reported as an ordinary bad request or rate limit, so only the body
    // separates it from a malformed request or a burst of traffic.
    private static bool IsOutOfCredit(ProviderFailure failure)
    {
        var body = failure.Body ?? string.Empty;
        var reportsSpentAccount = failure.Status is 400 or 402 or 429;

        if (!reportsSpentAccount)
        {
            return false;
        }

        return CreditMarkers.Any(marker => body.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    // A failure the mapping does not know still carries the provider's own reason, which says far
    // more than a generic apology.
    private static string? ReadReportedMessage(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }

        var start = body.IndexOf('{');

        if (start < 0)
        {
            return null;
        }

        var message = ReadErrorMessage(body[start..]);

        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var trimmed = message.Trim();
        var isOverLength = trimmed.Length > MaxReportedLength;

        return isOverLength ? $"{trimmed[..MaxReportedLength]}…" : trimmed;
    }

    private static string? ReadErrorMessage(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var hasError = root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object;

            if (!hasError)
            {
                return null;
            }

            var hasMessage = error.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.String;

            if (!hasMessage)
            {
                return null;
            }

            return message.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? DescribeStatus(int status)
    {
        return status switch
        {
            401 => InvalidKey,
            403 => Forbidden,
            404 => ModelMissing,
            413 or 422 => TooLarge,
            429 => RateLimited,
            >= 500 => Unavailable,
            _ => null,
        };
    }
}
