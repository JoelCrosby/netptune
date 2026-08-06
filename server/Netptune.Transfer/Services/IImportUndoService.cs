using Netptune.Transfer.Entities;

namespace Netptune.Transfer.Services;

public sealed record ImportUndoResult
{
    public int Reverted { get; init; }

    public IReadOnlyList<string> Blocked { get; init; } = [];
}

public interface IImportUndoService
{
    Task<ImportUndoResult> Undo(ImportSession session, string userId, CancellationToken cancellationToken = default);
}
