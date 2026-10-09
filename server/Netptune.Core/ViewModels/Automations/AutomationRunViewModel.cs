using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Automations;

public record AutomationRunViewModel
{
    public int Id { get; init; }

    public int AutomationRuleId { get; init; }

    public int? EntityId { get; init; }

    public EntityType? EntityType { get; init; }

    public string? TaskSystemId { get; init; }

    public string? TaskName { get; init; }

    public AutomationTriggerType TriggerType { get; init; }

    public AutomationRunStatus Status { get; init; }

    public string IdempotencyKey { get; init; } = null!;

    public string? Message { get; init; }

    public DateTime CreatedAt { get; init; }

    public List<AutomationActionResultViewModel> ActionResults { get; init; } = [];
}

public record AutomationRunSummaryViewModel
{
    public int TotalCount { get; init; }

    public int SucceededCount { get; init; }

    public int SkippedCount { get; init; }

    public int FailedCount { get; init; }

    public DateTime? LastRunAt { get; init; }
}
