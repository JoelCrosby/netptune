using System.ClientModel;
using System.Text.Json;

using Anthropic.Exceptions;

using Netptune.Core.Localization;

namespace Netptune.Ai.Providers;

public static class AiProviderErrors
{
    private const int MaxReportedLength = 240;

    private static string InvalidKey => AppText.AiProviderInvalidKey;

    private static string Forbidden => AppText.AiProviderForbidden;

    private static string OutOfCredit => AppText.AiProviderOutOfCredit;

    private static string RateLimited => AppText.AiProviderRateLimited;

    private static string ModelMissing => AppText.AiProviderModelMissing;

    private static string TooLarge => AppText.AiProviderTooLarge;

    private static string Rejected => AppText.AiProviderRejected;

    private static string Unavailable => AppText.AiProviderUnavailable;

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
