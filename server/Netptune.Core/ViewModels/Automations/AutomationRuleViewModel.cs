using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.ViewModels.Automations;

public record AutomationRuleViewModel
{
    public int Id { get; init; }

    public int WorkspaceId { get; init; }

    public string Name { get; init; } = null!;

    public bool IsEnabled { get; init; }

    public DateTime? AutoDisabledAt { get; init; }

    public string? AutoDisabledReason { get; init; }

    public string? ExecutionUserId { get; init; }

    public int? ProjectId { get; init; }

    public int? BoardId { get; init; }

    public int? SprintId { get; init; }

    public AutomationTriggerViewModel Trigger { get; init; } = null!;

    public List<AutomationActionViewModel> Actions { get; init; } = [];

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public List<AutomationRuleWarning> Warnings { get; init; } = [];
}

public record AutomationTriggerViewModel
{
    public AutomationTriggerType Type { get; init; }

    public List<TaskChangeField>? Fields { get; init; }

    public AutomationConditionGroup? ConditionGroup { get; init; }

    public int? DurationDays { get; init; }
}

public record AutomationActionViewModel : AutomationActionFields
{
    public int Id { get; init; }

    public int SortOrder { get; init; }
}

public record AutomationManualRunViewModel
{
    public int RuleId { get; init; }

    public int TaskCount { get; init; }
}

public record AutomationRuleListItemViewModel : AutomationRuleViewModel
{
    public AutomationRunViewModel? LastRun { get; init; }
}

public record AutomationRuleSummaryViewModel
{
    public int RuleCount { get; init; }

    public int EnabledCount { get; init; }

    public int RecentFailureCount { get; init; }
}
