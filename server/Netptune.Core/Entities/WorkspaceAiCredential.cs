using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record WorkspaceAiCredential : KeyedEntity<Guid>
{
    public int WorkspaceId { get; init; }

    public AiProvider Provider { get; init; }

    public required string Label { get; set; }

    public required byte[] Secret { get; set; }

    public required string SecretHint { get; set; }

    public string? Model { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime? LastUsedAt { get; set; }

    [JsonIgnore]
    public Workspace Workspace { get; init; } = null!;
}
