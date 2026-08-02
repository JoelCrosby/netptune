using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiUndoCatalog : IAiUndoCatalog
{
    private readonly HashSet<string> Undoable;

    public AiUndoCatalog(IEnumerable<IAiChangeHandler> handlers)
    {
        Undoable = handlers
            .Where(handler => handler is IAiChangeUndoHandler)
            .Select(handler => handler.ToolName)
            .ToHashSet(StringComparer.Ordinal);
    }

    public bool CanUndo(string toolName)
    {
        return Undoable.Contains(toolName);
    }
}
