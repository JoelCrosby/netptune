using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record UserAiCredential : KeyedEntity<Guid>
{
    public required string UserId { get; init; }

    public AiProvider Provider { get; init; }

    public required string Label { get; set; }

    public required byte[] Secret { get; set; }

    public required string SecretHint { get; set; }

    public string? Model { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime? LastUsedAt { get; set; }

    [JsonIgnore]
    public AppUser User { get; init; } = null!;
}
