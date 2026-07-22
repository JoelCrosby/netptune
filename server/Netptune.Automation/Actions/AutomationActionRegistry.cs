using Netptune.Core.Enums;
using Netptune.Core.Services.Automations;

namespace Netptune.Automation.Actions;

public sealed class AutomationActionRegistry : IAutomationActionRegistry
{
    private readonly IReadOnlyDictionary<AutomationActionType, IAutomationAction> Actions;

    public AutomationActionRegistry(IEnumerable<IAutomationAction> actions)
    {
        var actionList = actions.ToList();
        var duplicateTypes = actionList
            .GroupBy(action => action.Type)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateTypes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Multiple automation actions are registered for: {string.Join(", ", duplicateTypes)}.");
        }

        var registeredTypes = actionList.Select(action => action.Type).ToHashSet();
        var missingTypes = Enum.GetValues<AutomationActionType>()
            .Where(type => !registeredTypes.Contains(type))
            .ToList();

        if (missingTypes.Count > 0)
        {
            throw new InvalidOperationException(
                $"No automation action is registered for: {string.Join(", ", missingTypes)}.");
        }

        Actions = actionList.ToDictionary(action => action.Type);
    }

    public IAutomationAction? Find(AutomationActionType type)
    {
        return Actions.GetValueOrDefault(type);
    }
}
