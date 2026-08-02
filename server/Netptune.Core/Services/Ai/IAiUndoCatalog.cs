namespace Netptune.Core.Services.Ai;

public interface IAiUndoCatalog
{
    bool CanUndo(string toolName);
}
