using System.Text.Json;

namespace Netptune.Ai.Providers;

internal static class AnthropicReplayBlockTypes
{
    public const string Thinking = "thinking";

    public const string RedactedThinking = "redacted_thinking";

    public const string Text = "text";

    public const string ToolUse = "tool_use";
}

internal sealed record AnthropicReplayBlock
{
    public required string Type { get; init; }

    public string? Thinking { get; init; }

    public string? Signature { get; init; }

    public string? Data { get; init; }

    public string? ToolUseId { get; init; }
}

// An assistant turn's blocks in the order the API returned them, so the turn can go back exactly as it came.
// Only the thinking blocks carry their content; text and tool calls are filled in from the turn itself.
internal sealed record AnthropicReplay
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public List<AnthropicReplayBlock> Blocks { get; init; } = [];

    public JsonDocument ToDocument()
    {
        return JsonSerializer.SerializeToDocument(this, Json);
    }

    public static AnthropicReplay? Read(JsonDocument? payload)
    {
        if (payload is null)
        {
            return null;
        }

        try
        {
            return payload.Deserialize<AnthropicReplay>(Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
