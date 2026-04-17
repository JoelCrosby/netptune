using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record Notification : AuditableEntity<int>
{
    public string UserId { get; init; } = null!;

    public int ActivityLogId { get; init; }

    public bool IsRead { get; set; }

    public string? Link { get; init; }

    public int WorkspaceId { get; init; }

    public EntityType EntityType { get; init; }

    public ActivityType ActivityType { get; init; }

    #region NavigationProperties

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    [JsonIgnore]
    public ActivityLog ActivityLog { get; set; } = null!;

    [JsonIgnore]
    public Workspace Workspace { get; set; } = null!;

    #endregion
}
