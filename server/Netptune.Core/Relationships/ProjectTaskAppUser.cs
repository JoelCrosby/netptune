using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public class ProjectTaskAppUser : KeyedEntity<int>
{
    public int ProjectTaskId { get; set; }

    public string UserId { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ProjectTask ProjectTask { get; set; }

    [JsonIgnore]
    public AppUser User { get; set; }

    #endregion
}
