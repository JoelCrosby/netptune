namespace Netptune.Ai.Providers;

internal static class AiFinishReasons
{
    // Anthropic reports max_tokens and OpenAI reports length, and either SDK may hand the value over in
    // its own casing.
    public static bool IsOutputLimit(string? finishReason)
    {
        var normalised = Normalise(finishReason);

        return normalised is "maxtokens" or "length";
    }

    // Anthropic reports refusal and OpenAI reports content_filter.
    public static bool IsRefusal(string? finishReason)
    {
        var normalised = Normalise(finishReason);

        return normalised is "refusal" or "contentfilter";
    }

    private static string? Normalise(string? finishReason)
    {
        return finishReason?.Replace("_", string.Empty).ToLowerInvariant();
    }
}
