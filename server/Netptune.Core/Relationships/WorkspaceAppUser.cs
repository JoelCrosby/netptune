using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public class WorkspaceAppUser : KeyedEntity<int>
{
    public int WorkspaceId { get; set; }

    public string UserId { get; set; } = null!;

    #region NavigationProperties

    [JsonIgnore]
    public Workspace Workspace { get; set; } = null!;

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion
}
