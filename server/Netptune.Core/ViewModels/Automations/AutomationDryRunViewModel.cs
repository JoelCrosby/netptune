using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.ViewModels.Automations;

public sealed record AutomationDryRunViewModel
{
    public int RuleId { get; init; }

    public required string RuleName { get; init; }

    public bool IsEnabled { get; init; }

    public AutomationTriggerType TriggerType { get; init; }

    public int TaskId { get; init; }

    public required string TaskName { get; init; }

    public bool ConditionsMatch { get; init; }

    public bool HasUnevaluableConditions { get; init; }

    public AutomationConditionGroupExplanation? ConditionGroup { get; init; }
}
