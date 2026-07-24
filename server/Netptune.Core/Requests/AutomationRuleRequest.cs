using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.Requests;

public record AutomationRuleRequest
{
    public string Name { get; init; } = null!;

    public bool IsEnabled { get; init; } = true;

    public string? ExecutionUserId { get; init; }

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
        var hasConditionGroup = ConditionGroup is not null;
        var supportsConditions = Type == AutomationTriggerType.TaskChanged;
        var hasUnsupportedConditions = hasConditionGroup && !supportsConditions;

        if (hasUnsupportedConditions)
        {
            return "Field conditions are only supported for task changed automations.";
        }

        if (ConditionGroup is not null)
        {
            return ConditionGroup.Validate();
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

    public string? TaskName { get; init; }

    public string? TaskDescription { get; init; }

    public bool ClearDescription { get; init; }

    public string? OwnerId { get; init; }

    public bool ClearOwner { get; init; }

    public List<string>? AssigneeIds { get; init; }

    public List<string> AddTags { get; init; } = [];

    public List<string> RemoveTags { get; init; } = [];

    public AutomationDateUpdate? StartDate { get; init; }

    public AutomationDateUpdate? DueDate { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public bool ClearEstimate { get; init; }

    public int? SprintId { get; init; }

    public bool ClearSprint { get; init; }

    public int? BoardGroupId { get; init; }

    public int? DelayAmount { get; init; }

    public AutomationDelayUnit? DelayUnit { get; init; }
}
