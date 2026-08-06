using System.Collections.Frozen;

namespace Netptune.Transfer.Undo;

public sealed class EntityUndoCatalog
{
    private readonly FrozenDictionary<string, IEntityUndoHandler> HandlersByType;

    public EntityUndoCatalog(IEnumerable<IEntityUndoHandler> handlers)
    {
        HandlersByType = handlers.ToFrozenDictionary(handler => handler.EntityType, StringComparer.OrdinalIgnoreCase);
    }

    public IEntityUndoHandler? Resolve(string entityType)
    {
        return HandlersByType.GetValueOrDefault(entityType);
    }

    public bool CanUndo(string entityType)
    {
        return HandlersByType.ContainsKey(entityType);
    }
}
