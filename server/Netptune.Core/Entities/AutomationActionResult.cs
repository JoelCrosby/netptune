using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record AutomationActionResult : AuditableEntity<int>
{
    public int AutomationRunId { get; set; }

    public int AutomationActionId { get; set; }

    public AutomationActionType ActionType { get; set; }

    public int SortOrder { get; set; }

    public AutomationActionResultStatus Status { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Message { get; set; }

    public JsonDocument? Output { get; set; }

    [JsonIgnore]
    public AutomationRun AutomationRun { get; set; } = null!;
}
