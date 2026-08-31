using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.Requests;

public record AutomationRuleRequest
{
    public string Name { get; init; } = null!;

    public bool IsEnabled { get; init; } = true;

    public string? ExecutionUserId { get; init; }

    public int? ProjectId { get; init; }

    public int? BoardId { get; init; }

    public int? SprintId { get; init; }

    public AutomationTriggerRequest Trigger { get; init; } = null!;

    public List<AutomationActionRequest> Actions { get; init; } = [];
}

public record AutomationTriggerRequest
{
    public AutomationTriggerType Type { get; init; }

    public List<TaskChangeField>? Fields { get; init; }

    public AutomationConditionGroup? ConditionGroup { get; init; }

    public int? DurationDays { get; init; }

    public string? Validate()
    {
        var hasWatchedFields = Fields is { Count: > 0 };
        var hasValidUnassignedDuration = DurationDays is >= 1 and <= 365;
        var hasValidDueDateDuration = DurationDays is >= 0 and <= 365;
        var hasValidInactiveDuration = DurationDays is >= 1 and <= 365;
        var hasValidSprintEndingDuration = DurationDays is >= 0 and <= 365;
        var hasSupportedType = Enum.IsDefined(Type);

        if (!hasSupportedType)
        {
            return $"Automation trigger type '{Type}' is not supported.";
        }

        var triggerError = Type switch
        {
            AutomationTriggerType.TaskChanged when !hasWatchedFields =>
                "Task changed automations require at least one field.",
            AutomationTriggerType.TaskUnassignedFor when !hasValidUnassignedDuration =>
                "Task unassigned automations require durationDays between 1 and 365.",
            AutomationTriggerType.TaskDueDateApproaching when !hasValidDueDateDuration =>
                "Task due-date automations require durationDays between 0 and 365.",
            AutomationTriggerType.TaskInactiveFor when !hasValidInactiveDuration =>
                "Task inactivity automations require durationDays between 1 and 365.",
            AutomationTriggerType.SprintEndingSoon when !hasValidSprintEndingDuration =>
                "Sprint ending automations require durationDays between 0 and 365.",
            _ => null,
        };

        if (triggerError is not null)
        {
            return triggerError;
        }

        return ValidateConditions();
    }

    private string? ValidateConditions()
    {
        if (ConditionGroup is not null)
        {
            var supportsChangeOperators = Type == AutomationTriggerType.TaskChanged;

            return ConditionGroup.Validate(supportsChangeOperators);
        }

        return null;
    }
}

public record AutomationActionRequest : AutomationActionFields;

public record AutomationManualRunRequestBody
{
    public List<int> TaskIds { get; init; } = [];
}

public record AutomationCloneRequest
{
    public string? Name { get; init; }
}
