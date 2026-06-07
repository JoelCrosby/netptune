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
            AutomationTriggerType.TaskStatusChanged when request.Trigger.Status is null =>
                "Task status changed automations require a status.",
            AutomationTriggerType.TaskUnassignedFor when request.Trigger.DurationDays is null or < 1 or > 365 =>
                "Task unassigned automations require durationDays between 1 and 365.",
            _ => null,
        };

        if (triggerError is not null) return triggerError;

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
                _ => null,
            };

            if (actionError is not null) return actionError;
        }

        return null;
    }
}
