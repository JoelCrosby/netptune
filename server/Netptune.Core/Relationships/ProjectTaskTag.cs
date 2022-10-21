using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public record ProjectTaskTag : KeyedEntity<int>
{
    public int ProjectTaskId { get; set; }

    public int TagId { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ProjectTask ProjectTask { get; set; } = null!;

    [JsonIgnore]
    public Tag Tag { get; set; } = null!;

    #endregion
}
