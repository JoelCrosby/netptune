using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.Enums;

namespace Netptune.Core.Models.Ai;

public sealed record AiMessageContentToolCall
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Arguments { get; init; }
}

public sealed record AiMessageContentToolResult
{
    public required string ToolCallId { get; init; }

    public required string Content { get; init; }

    public bool IsError { get; init; }
}

public sealed record AiMessageContent
{
    public string? Text { get; init; }

    public List<AiMessageContentToolCall> ToolCalls { get; init; } = [];

    public List<AiMessageContentToolResult> ToolResults { get; init; } = [];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static AiMessageContent FromChatMessage(AiChatMessage message)
    {
        return new AiMessageContent
        {
            Text = message.Text,
            ToolCalls = message.ToolCalls
                .Select(call => new AiMessageContentToolCall
                {
                    Id = call.Id,
                    Name = call.Name,
                    Arguments = call.Arguments.RootElement.GetRawText(),
                })
                .ToList(),
            ToolResults = message.ToolResults
                .Select(result => new AiMessageContentToolResult
                {
                    ToolCallId = result.ToolCallId,
                    Content = result.Content,
                    IsError = result.IsError,
                })
                .ToList(),
        };
    }

    public AiChatMessage ToChatMessage(AiMessageRole role)
    {
        return new AiChatMessage
        {
            Role = role,
            Text = Text,
            ToolCalls = ToolCalls
                .Select(call => new AiToolCall
                {
                    Id = call.Id,
                    Name = call.Name,
                    Arguments = JsonDocument.Parse(call.Arguments),
                })
                .ToList(),
            ToolResults = ToolResults
                .Select(result => new AiToolResult
                {
                    ToolCallId = result.ToolCallId,
                    Content = result.Content,
                    IsError = result.IsError,
                })
                .ToList(),
        };
    }

    public JsonDocument ToJsonDocument()
    {
        var json = JsonSerializer.Serialize(this, SerializerOptions);

        return JsonDocument.Parse(json);
    }

    public static AiMessageContent FromJsonDocument(JsonDocument document)
    {
        var content = document.Deserialize<AiMessageContent>(SerializerOptions);

        return content ?? new AiMessageContent();
    }
}
