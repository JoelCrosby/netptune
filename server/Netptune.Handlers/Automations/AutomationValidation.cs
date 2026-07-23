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

        var triggerError = request.Trigger.Validate();

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
