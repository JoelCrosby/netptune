using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record AiToolInvocation : KeyedEntity<long>
{
    public Guid ConversationId { get; init; }

    public long MessageId { get; init; }

    public required string ToolName { get; init; }

    public JsonDocument Arguments { get; init; } = JsonDocument.Parse("{}");

    public JsonDocument? Result { get; init; }

    public bool ResultTruncated { get; init; }

    public AiToolInvocationStatus Status { get; init; }

    public string? Error { get; init; }

    public int DurationMilliseconds { get; init; }

    public DateTime CreatedAt { get; init; }

    [JsonIgnore]
    public AiMessage Message { get; init; } = null!;
}
