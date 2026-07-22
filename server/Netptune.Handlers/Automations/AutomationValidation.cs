using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Handlers.Automations;

internal static class AutomationValidation
{
    public static string? Validate(AutomationRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Automation name is required.";
        }

        var triggerError = request.Trigger.Type switch
        {
            AutomationTriggerType.TaskChanged when request.Trigger.Fields is null || request.Trigger.Fields.Count == 0 =>
                "Task changed automations require at least one field.",
            AutomationTriggerType.TaskChanged when request.Trigger.StatusId is not null &&
                                                    !request.Trigger.Fields.Contains(TaskChangeField.Status) =>
                "Task changed automations can only set status when watching the status field.",
            AutomationTriggerType.TaskChanged when request.Trigger.AssigneeChangeMode is not null &&
                                                    !request.Trigger.Fields.Contains(TaskChangeField.Assignees) =>
                "Task changed automations can only set assigneeChangeMode when watching the assignees field.",
            AutomationTriggerType.TaskStatusChanged when request.Trigger.StatusId is null =>
                "Task status changed automations require a status.",
            AutomationTriggerType.TaskUnassignedFor when request.Trigger.DurationDays is null or < 1 or > 365 =>
                "Task unassigned automations require durationDays between 1 and 365.",
            _ => null,
        };

        if (triggerError is not null)
        {
            return triggerError;
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
            var actionError = action.Type switch
            {
                AutomationActionType.FlagTask when string.IsNullOrWhiteSpace(action.FlagName) =>
                    "Flag task actions require flagName.",
                AutomationActionType.AddComment when string.IsNullOrWhiteSpace(action.Comment) =>
                    "Add comment actions require comment.",
                AutomationActionType.AddComment when action.Comment is { Length: > 32768 } =>
                    "Add comment actions cannot exceed 32768 characters.",
                AutomationActionType.UpdateTask when action.StatusId is null && action.Priority is null =>
                    "Update task actions require status or priority.",
                _ => null,
            };

            if (actionError is not null)
            {
                return actionError;
            }
        }

        return null;
    }
}
