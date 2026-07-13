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
    public Workspace Workspace { get; set; } = null!;

    [JsonIgnore]
    public RelationType RelationType { get; set; } = null!;

    [JsonIgnore]
    public ProjectTask SourceTask { get; set; } = null!;

    [JsonIgnore]
    public ProjectTask TargetTask { get; set; } = null!;

    #endregion
}
