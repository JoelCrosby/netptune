using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiMessage : KeyedEntity<long>
{
    public Guid ConversationId { get; init; }

    public int Sequence { get; init; }

    public AiMessageRole Role { get; init; }

    public JsonDocument Content { get; init; } = JsonDocument.Parse("[]");

    public JsonDocument? ProviderPayload { get; init; }

    public AiProvider Provider { get; init; }

    public required string Model { get; init; }

    public AiMessageStatus Status { get; init; }

    public string? FinishReason { get; init; }

    public string? Error { get; init; }

    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public int CacheReadTokens { get; init; }

    public int CacheCreationTokens { get; init; }

    public DateTime CreatedAt { get; init; }

    [JsonIgnore]
    public AiConversation Conversation { get; init; } = null!;
}
