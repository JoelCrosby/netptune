using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public class Flag : WorkspaceEntity<int>
{
    public string Name { get; set; }

    public string Description { get; set; }
}