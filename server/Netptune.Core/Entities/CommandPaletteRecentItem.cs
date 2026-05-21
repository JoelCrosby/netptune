using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record CommandPaletteRecentItem : KeyedEntity<int>
{
    public required string UserId { get; set; }

    public int WorkspaceId { get; set; }

    public required string Type { get; set; }

    public string? EntityId { get; set; }

    public required string Title { get; set; }

    public required string Url { get; set; }

    public DateTime LastAccessedAt { get; set; }

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    [JsonIgnore]
    public Workspace Workspace { get; set; } = null!;
}
