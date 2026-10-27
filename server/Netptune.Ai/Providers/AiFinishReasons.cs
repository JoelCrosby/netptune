namespace Netptune.Ai.Providers;

internal static class AiFinishReasons
{
    // Anthropic reports max_tokens and OpenAI reports length, and either SDK may hand the value over in
    // its own casing.
    public static bool IsOutputLimit(string? finishReason)
    {
        var normalised = finishReason?.Replace("_", string.Empty).ToLowerInvariant();

        return normalised is "maxtokens" or "length";
    }
}
