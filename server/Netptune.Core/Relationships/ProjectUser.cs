using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public class ProjectUser : KeyedEntity<int>
{
    public int ProjectId { get; set; }

    public string UserId { get; set; } = null!;

    #region NavigationProperties

    [JsonIgnore]
    public Project Project { get; set; } = null!;

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion
}
