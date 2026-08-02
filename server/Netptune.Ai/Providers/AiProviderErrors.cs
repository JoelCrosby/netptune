using System.ClientModel;

using Anthropic.Exceptions;

namespace Netptune.Ai.Providers;

public static class AiProviderErrors
{
    private const string InvalidKey =
        "The provider rejected the API key. Check the key in your assistant settings.";

    private const string Forbidden =
        "The API key is not allowed to use this model. Pick another model or use a key with access to it.";

    private const string RateLimited =
        "The provider is rate limiting this key, or the account is out of credit. Try again shortly.";

    private const string ModelMissing =
        "The provider does not know the selected model. Pick a different model in the assistant settings.";

    private const string TooLarge =
        "The conversation is too large for this model. Start a new chat and try again.";

    private const string Unavailable =
        "The provider is unavailable right now. Try again shortly.";

    public static string? Describe(Exception exception)
    {
        return exception switch
        {
            AnthropicUnauthorizedException => InvalidKey,
            AnthropicForbiddenException => Forbidden,
            AnthropicRateLimitException => RateLimited,
            AnthropicNotFoundException => ModelMissing,
            AnthropicUnprocessableEntityException => TooLarge,
            AnthropicServiceException => Unavailable,
            ClientResultException result => DescribeStatus(result.Status),
            _ => null,
        };
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
