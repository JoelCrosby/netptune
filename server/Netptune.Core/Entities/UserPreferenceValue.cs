using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record UserPreferenceValue : KeyedEntity<int>
{
    public required string UserId { get; set; }

    public int? WorkspaceId { get; set; }

    public required string Key { get; set; }

    public required JsonDocument Value { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    [JsonIgnore]
    public Workspace? Workspace { get; set; }
}
