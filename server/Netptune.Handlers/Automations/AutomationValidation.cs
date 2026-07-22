using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;

namespace Netptune.Handlers.Automations;

internal static class AutomationValidation
{
    public static string? Validate(AutomationRuleRequest request, IAutomationActionRegistry actionRegistry)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Automation name is required.";
        }

        var triggerError = request.Trigger.Type switch
        {
            AutomationTriggerType.TaskChanged when request.Trigger.Fields is null || request.Trigger.Fields.Count == 0 =>
                "Task changed automations require at least one field.",
            AutomationTriggerType.TaskChanged when request.Trigger.StatusId is not null && !request.Trigger.Fields.Contains(TaskChangeField.Status) =>
                "Task changed automations can only set status when watching the status field.",
            AutomationTriggerType.TaskChanged when request.Trigger.AssigneeChangeMode is not null && !request.Trigger.Fields.Contains(TaskChangeField.Assignees) =>
                "Task changed automations can only set assigneeChangeMode when watching the assignees field.",
            AutomationTriggerType.TaskStatusChanged when request.Trigger.StatusId is null =>
                "Task status changed automations require a status.",
            AutomationTriggerType.TaskUnassignedFor when request.Trigger.DurationDays is null or < 1 or > 365 =>
                "Task unassigned automations require durationDays between 1 and 365.",
            AutomationTriggerType.TaskDueDateApproaching when request.Trigger.DurationDays is null or < 0 or > 365 =>
                "Task due-date automations require durationDays between 0 and 365.",
            _ => null,
        };

        if (triggerError is not null)
        {
            return triggerError;
        }

        var conditionError = ValidateConditions(request.Trigger);

        if (conditionError is not null)
        {
            return conditionError;
        }

        if (request.Actions.Count == 0)
        {
            return "At least one automation action is required.";
        }

        if (request.Actions.Count > 10)
        {
            return "Automations cannot have more than 10 actions.";
        }

        foreach (var action in request.Actions)
        {
            var automationAction = actionRegistry.Find(action.Type);

            if (automationAction is null)
            {
                return $"Automation action type '{action.Type}' is not supported.";
            }

            var actionError = automationAction.Validate(action);

            if (actionError is not null)
            {
                return actionError;
            }
        }

        return null;
    }

    private static string? ValidateConditions(AutomationTriggerRequest trigger)
    {
        var conditions = trigger.Conditions ?? [];

        if (trigger.Type != AutomationTriggerType.TaskChanged && conditions.Count > 0)
        {
            return "Field conditions are only supported for task changed automations.";
        }

        if (conditions.Count != conditions.Select(condition => condition.Field).Distinct().Count())
        {
            return "Task changed automations can only configure one condition per field.";
        }

        foreach (var condition in conditions)
        {
            if (trigger.Fields is null || !trigger.Fields.Contains(condition.Field))
            {
                return $"Condition field '{condition.Field}' must be included in fields.";
            }

            var error = ValidateCondition(condition);

            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    private static string? ValidateCondition(AutomationFieldCondition condition)
    {
        var isCollection = condition.Field is
            TaskChangeField.Assignees or
            TaskChangeField.Tags;

        var isText = condition.Field is
            TaskChangeField.Name or
            TaskChangeField.Description;

        var isCollectionOperator = condition.Operator is
            AutomationConditionOperator.Added or
            AutomationConditionOperator.Removed;

        var isScalarOperator = condition.Operator is
            AutomationConditionOperator.Equals or
            AutomationConditionOperator.NotEquals or
            AutomationConditionOperator.IsEmpty or
            AutomationConditionOperator.IsNotEmpty;

        var isSupported = condition.Operator == AutomationConditionOperator.Any ||
            isCollection && isCollectionOperator ||
            !isCollection && (isScalarOperator || isText && condition.Operator == AutomationConditionOperator.Contains);

        if (!isSupported)
        {
            return $"Operator '{condition.Operator}' is not supported for field '{condition.Field}'.";
        }

        var requiresValue = condition.Operator is
            AutomationConditionOperator.Equals or
            AutomationConditionOperator.NotEquals or
            AutomationConditionOperator.Contains;

        if (requiresValue && string.IsNullOrWhiteSpace(condition.Value))
        {
            return $"Condition operator '{condition.Operator}' requires a value.";
        }

        return ValidateConditionValue(condition);
    }

    private static string? ValidateConditionValue(AutomationFieldCondition condition)
    {
        if (string.IsNullOrWhiteSpace(condition.Value))
        {
            return null;
        }

        var hasValidValue = condition.Field switch
        {
            TaskChangeField.Status => int.TryParse(condition.Value, out _),
            TaskChangeField.Priority => Enum.TryParse<TaskPriority>(condition.Value, true, out _),
            TaskChangeField.Estimate => Enum.TryParse<EstimateType>(condition.Value, true, out _),
            TaskChangeField.StartDate or TaskChangeField.DueDate => DateOnly.TryParse(condition.Value, out _),
            _ => true,
        };

        return hasValidValue ? null : $"Condition value '{condition.Value}' is invalid for field '{condition.Field}'.";
    }
}
