using Netptune.Core.Enums;

namespace Netptune.Automation.Execution.Actions;

internal sealed class ScheduledActionHandlerRegistry
{
    private readonly Dictionary<AutomationActionType, IScheduledActionHandler> Handlers;

    public ScheduledActionHandlerRegistry(IEnumerable<IScheduledActionHandler> handlers)
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
                $"Multiple scheduled automation action handlers are registered for: {string.Join(", ", duplicateTypes)}.");
        }

        Handlers = handlerList.ToDictionary(handler => handler.Type);
    }

    internal IScheduledActionHandler? Find(AutomationActionType type)
    {
        return Handlers.GetValueOrDefault(type);
    }
}
