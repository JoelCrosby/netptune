using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record Flag : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public EntityType? EntityType { get; set; }

    public int? EntityId { get; set; }

    public int? AutomationRuleId { get; set; }
}
