using System.Text.Json.Serialization;
using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public record TaskFile : KeyedEntity<int>
{
    public int WorkspaceId { get; set; }

    public int ProjectTaskId { get; set; }

    public int WorkspaceFileId { get; set; }

    [JsonIgnore]
    public Workspace Workspace { get; set; } = null!;

    [JsonIgnore]
    public ProjectTask ProjectTask { get; set; } = null!;

    [JsonIgnore]
    public WorkspaceFile WorkspaceFile { get; set; } = null!;
}
