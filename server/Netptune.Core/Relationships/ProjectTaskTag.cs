using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public class ProjectTaskTag : KeyedEntity<int>
{
    public int ProjectTaskId { get; set; }

    public int TagId { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ProjectTask ProjectTask { get; set; }

    [JsonIgnore]
    public Tag Tag { get; set; }

    #endregion
}