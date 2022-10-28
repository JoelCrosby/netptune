using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record ActivityLog : WorkspaceEntity<int>
{
    public EntityType EntityType { get; init; }

    public string UserId { get; init; } = null!;

    public ActivityType Type { get; init; }

    public int? EntityId { get; init; }

    public DateTime Time { get; init; }

    public int? ProjectId { get; init; }

    public int? BoardId { get; init; }

    public int? BoardGroupId { get; init; }

    public int? TaskId { get; init; }

    public JsonDocument? Meta { get; init; }

    #region NavigationProperties

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion
}
