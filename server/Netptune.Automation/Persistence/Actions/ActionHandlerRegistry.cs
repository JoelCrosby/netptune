using Netptune.Core.Enums;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class ActionHandlerRegistry
{
    private readonly Dictionary<AutomationActionType, IActionExecutionHandler> Handlers;

    public ActionHandlerRegistry(IEnumerable<IActionExecutionHandler> handlers)
    {
        var handlerList = handlers.ToList();
        var duplicateTypes = handlerList
            .GroupBy(handler => handler.Type)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateTypes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Multiple automation action execution handlers are registered for: {string.Join(", ", duplicateTypes)}.");
        }

        var registeredTypes = handlerList.Select(handler => handler.Type).ToHashSet();
        var missingTypes = Enum.GetValues<AutomationActionType>()
            .Where(type => !registeredTypes.Contains(type))
            .ToList();

        if (missingTypes.Count > 0)
        {
            throw new InvalidOperationException(
                $"No automation action execution handler is registered for: {string.Join(", ", missingTypes)}.");
        }

        Handlers = handlerList.ToDictionary(handler => handler.Type);
    }

    internal IActionExecutionHandler? Find(AutomationActionType type)
    {
        return Handlers.GetValueOrDefault(type);
    }
}
