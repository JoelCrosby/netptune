using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record ActivityEntry : WorkspaceEntity<int>
{
    public string? WorkspaceSlug { get; init; }

    public EntityType EntityType { get; init; }

    public int EntityId { get; init; }

    public string UserId { get; init; } = null!;

    public ActivityType ActivityType { get; set; }

    public List<string> ChangedFields { get; set; } = [];

    public JsonDocument? Meta { get; set; }

    public int LastActivityLogId { get; set; }

    public DateTime FirstOccurredAt { get; init; }

    public DateTime LastOccurredAt { get; set; }

    public int RevisionCount { get; set; }

    public bool IsOpen { get; set; }

    public DateTime WindowExpiresAt { get; set; }

    public DateTime? NotifiedAt { get; set; }

    #region DenormalisedAncestors

    public int? ProjectId { get; init; }

    public string? ProjectSlug { get; init; }

    public int? BoardId { get; init; }

    public string? BoardSlug { get; init; }

    public int? BoardGroupId { get; init; }

    public int? TaskId { get; init; }

    #endregion

    #region NavigationProperties

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion
}
