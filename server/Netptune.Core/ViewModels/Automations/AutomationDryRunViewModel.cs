using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.ViewModels.Automations;

public sealed record AutomationDryRunActionViewModel
{
    public int ActionId { get; init; }

    public AutomationActionType Type { get; init; }

    public bool HasEffect { get; init; }

    public string? Message { get; init; }

    public List<string> RecipientUserIds { get; init; } = [];

    public bool IncludeProjectMembers { get; init; }

    public List<WorkspaceRole> RecipientRoles { get; init; } = [];

    public string? Comment { get; init; }

    public string? FlagName { get; init; }

    public List<string> UpdatedFields { get; init; } = [];

    public string? CreatedTaskName { get; init; }

    public int? DelayMinutes { get; init; }
}

public sealed record AutomationDryRunViewModel
{
    public int RuleId { get; init; }

    public required string RuleName { get; init; }

    public bool IsEnabled { get; init; }

    public AutomationTriggerType TriggerType { get; init; }

    public int TaskId { get; init; }

    public required string TaskName { get; init; }

    public bool TriggerMatches { get; init; }

    public bool TriggerIsEvaluable { get; init; }

    public bool ConditionsMatch { get; init; }

    public bool HasUnevaluableConditions { get; init; }

    public AutomationConditionGroupExplanation? ConditionGroup { get; init; }

    public List<AutomationDryRunActionViewModel> Actions { get; init; } = [];
}
