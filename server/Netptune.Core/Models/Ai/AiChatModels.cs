using System.Text.Json;

using Netptune.Core.Enums;

namespace Netptune.Core.Models.Ai;

public sealed record AiToolDefinition
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required JsonDocument InputSchema { get; init; }
}

public sealed record AiToolCall
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required JsonDocument Arguments { get; init; }
}

public sealed record AiToolResult
{
    public required string ToolCallId { get; init; }

    public required string Content { get; init; }

    public bool IsError { get; init; }
}

public sealed record AiChatMessage
{
    public AiMessageRole Role { get; init; }

    public string? Text { get; init; }

    public List<AiToolCall> ToolCalls { get; init; } = [];

    public List<AiToolResult> ToolResults { get; init; } = [];

    public JsonDocument? ProviderPayload { get; init; }
}

public sealed record AiUsage
{
    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public int CacheReadTokens { get; init; }

    public int CacheCreationTokens { get; init; }
}

public sealed record AiChatRequest
{
    public required string Model { get; init; }

    public required string SystemPrompt { get; init; }

    public required IReadOnlyList<AiChatMessage> Messages { get; init; }

    public required IReadOnlyList<AiToolDefinition> Tools { get; init; }

    public int MaxOutputTokens { get; init; } = 16000;
}

public sealed record AiChatTurn
{
    public string Text { get; init; } = string.Empty;

    public List<AiToolCall> ToolCalls { get; init; } = [];

    public JsonDocument? ProviderPayload { get; init; }

    public AiUsage Usage { get; init; } = new();

    public string? FinishReason { get; init; }
}
