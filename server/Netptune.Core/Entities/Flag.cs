using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record Flag : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
}
