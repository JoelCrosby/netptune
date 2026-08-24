using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record TaskPin : WorkspaceEntity<int>
{
    public int ProjectTaskId { get; set; }

    public TaskPinScope Scope { get; set; }

    // The id of the thing pinned to: board id, project id, or workspace id. User-scoped pins
    // carry the workspace id — a personal pin does not cross workspaces.
    public int ScopeEntityId { get; set; }

    public double SortOrder { get; set; }
}
