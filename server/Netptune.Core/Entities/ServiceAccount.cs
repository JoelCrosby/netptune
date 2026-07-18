using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public sealed record ServiceAccount : KeyedEntity<int>
{
    public required string UserId { get; init; }

    public int WorkspaceId { get; init; }

    public string? Description { get; set; }

    public required string CreatedByUserId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? DisabledAt { get; set; }

    [JsonIgnore]
    public AppUser User { get; init; } = null!;

    [JsonIgnore]
    public AppUser CreatedByUser { get; init; } = null!;

    [JsonIgnore]
    public Workspace Workspace { get; init; } = null!;

    [JsonIgnore]
    public ICollection<ServiceAccountOwner> Owners { get; init; } = new HashSet<ServiceAccountOwner>();

    [JsonIgnore]
    public ICollection<ApiCredential> Credentials { get; init; } = new HashSet<ApiCredential>();
}
