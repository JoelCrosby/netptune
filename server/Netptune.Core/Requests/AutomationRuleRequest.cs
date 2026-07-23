using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.Requests;

public record AutomationRuleRequest
{
    public string Name { get; init; } = null!;

    public bool IsEnabled { get; init; } = true;

    public AutomationTriggerRequest Trigger { get; init; } = null!;

    public List<AutomationActionRequest> Actions { get; init; } = [];
}

public record AutomationTriggerRequest
{
    public AutomationTriggerType Type { get; init; }

    public List<TaskChangeField>? Fields { get; init; }

    public List<AutomationFieldCondition>? Conditions { get; init; }

    public AutomationConditionGroup? ConditionGroup { get; init; }

    public int? StatusId { get; init; }

    public AssigneeChangeMode? AssigneeChangeMode { get; init; }

    public int? DurationDays { get; init; }

    public string? Validate()
    {
        var hasWatchedFields = Fields is { Count: > 0 };
        var watchesStatus = Fields?.Contains(TaskChangeField.Status) == true;
        var watchesAssignees = Fields?.Contains(TaskChangeField.Assignees) == true;
        var statusConfigurationRequiresStatusField = StatusId is not null && !watchesStatus;
        var assigneeConfigurationRequiresAssigneeField = AssigneeChangeMode is not null && !watchesAssignees;
        var hasValidUnassignedDuration = DurationDays is >= 1 and <= 365;
        var hasValidDueDateDuration = DurationDays is >= 0 and <= 365;

        var triggerError = Type switch
        {
            AutomationTriggerType.TaskChanged when !hasWatchedFields =>
                "Task changed automations require at least one field.",
            AutomationTriggerType.TaskChanged when statusConfigurationRequiresStatusField =>
                "Task changed automations can only set status when watching the status field.",
            AutomationTriggerType.TaskChanged when assigneeConfigurationRequiresAssigneeField =>
                "Task changed automations can only set assigneeChangeMode when watching the assignees field.",
            AutomationTriggerType.TaskStatusChanged when StatusId is null =>
                "Task status changed automations require a status.",
            AutomationTriggerType.TaskUnassignedFor when !hasValidUnassignedDuration =>
                "Task unassigned automations require durationDays between 1 and 365.",
            AutomationTriggerType.TaskDueDateApproaching when !hasValidDueDateDuration =>
                "Task due-date automations require durationDays between 0 and 365.",
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
        var conditions = Conditions ?? [];
        var hasLegacyConditions = conditions.Count > 0;
        var hasConditionGroup = ConditionGroup is not null;
        var hasAnyConditions = hasLegacyConditions || hasConditionGroup;
        var supportsConditions = Type == AutomationTriggerType.TaskChanged;
        var hasUnsupportedConditions = hasAnyConditions && !supportsConditions;
        var hasConflictingConditionFormats = hasLegacyConditions && hasConditionGroup;

        if (hasUnsupportedConditions)
        {
            return "Field conditions are only supported for task changed automations.";
        }

        if (hasConflictingConditionFormats)
        {
            return "Task changed automations cannot combine legacy conditions with a condition group.";
        }

        if (ConditionGroup is not null)
        {
            return ConditionGroup.Validate();
        }

        var conditionFields = conditions.Select(condition => condition.Field).ToList();
        var uniqueConditionFieldCount = conditionFields.Distinct().Count();
        var hasDuplicateConditionFields = conditionFields.Count != uniqueConditionFieldCount;

        if (hasDuplicateConditionFields)
        {
            return "Task changed automations can only configure one condition per field.";
        }

        foreach (var condition in conditions)
        {
            var fieldIsWatched = Fields?.Contains(condition.Field) == true;

            if (!fieldIsWatched)
            {
                return $"Condition field '{condition.Field}' must be included in fields.";
            }

            var error = condition.Validate();

            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }
}

public record AutomationActionRequest
{
    public AutomationActionType Type { get; init; }

    public string? Message { get; init; }

    public string? Comment { get; init; }

    public string? FlagName { get; init; }

    public string? FlagDescription { get; init; }

    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }

    public int? DelayAmount { get; init; }

    public AutomationDelayUnit? DelayUnit { get; init; }
}
