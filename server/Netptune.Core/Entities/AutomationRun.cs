using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record AutomationRun : AuditableEntity<int>
{
    public int AutomationRuleId { get; set; }

    public int? EntityId { get; set; }

    public EntityType? EntityType { get; set; }

    public AutomationTriggerType TriggerType { get; set; }

    public AutomationRunStatus Status { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public string? Message { get; set; }

    public JsonDocument? Context { get; set; }

    [JsonIgnore]
    public AutomationRule AutomationRule { get; set; } = null!;
}
