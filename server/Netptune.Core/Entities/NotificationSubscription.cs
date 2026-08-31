using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record NotificationSubscription : WorkspaceEntity<int>
{
    public string UserId { get; set; } = null!;

    public NotificationScope Scope { get; set; }

    // The id of the thing subscribed to: project, board, board group or sprint id.
    public int ScopeEntityId { get; set; }

    public NotificationSubscriptionEvents Events { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion
}
