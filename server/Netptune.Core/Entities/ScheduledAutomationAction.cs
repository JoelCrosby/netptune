using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public sealed record ScheduledAutomationAction : AuditableEntity<int>
{
    public int AutomationRuleId { get; set; }

    public int AutomationActionId { get; set; }

    public int TaskId { get; set; }

    public AutomationActionType ActionType { get; set; }

    public ScheduledAutomationActionStatus Status { get; set; }

    public int ExpectedStatusId { get; set; }

    public DateTime ExecuteAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int AttemptCount { get; set; }

    public Guid? ClaimId { get; set; }

    public DateTime? LeaseExpiresAt { get; set; }

    public string? LastError { get; set; }

    public JsonDocument? TriggerContext { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    [JsonIgnore]
    public AutomationRule AutomationRule { get; set; } = null!;

    [JsonIgnore]
    public AutomationAction AutomationAction { get; set; } = null!;

    [JsonIgnore]
    public ProjectTask Task { get; set; } = null!;
}
