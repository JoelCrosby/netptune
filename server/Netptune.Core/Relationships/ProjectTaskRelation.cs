using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public record ProjectTaskRelation : KeyedEntity<int>
{
    public int WorkspaceId { get; set; }

    public int RelationTypeId { get; set; }

    public int SourceTaskId { get; set; }

    public int TargetTaskId { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public Workspace? Workspace { get; set; }

    [JsonIgnore]
    public RelationType? RelationType { get; set; }

    [JsonIgnore]
    public ProjectTask? SourceTask { get; set; }

    [JsonIgnore]
    public ProjectTask? TargetTask { get; set; }

    #endregion
}
