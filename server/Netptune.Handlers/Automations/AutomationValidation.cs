using Netptune.Core.Enums;
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
}
