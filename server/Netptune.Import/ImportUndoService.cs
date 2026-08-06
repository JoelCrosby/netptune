using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Services;
using Netptune.Transfer.Undo;
using Netptune.Core.UnitOfWork;

namespace Netptune.Import;

public sealed class ImportUndoService : IImportUndoService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;
    private readonly EntityUndoCatalog Catalog;

    public ImportUndoService(INetptuneUnitOfWork unitOfWork, EntityUndoCatalog undo,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Catalog = undo;
        ImportSessions = importSessions;
    }

    public async Task<ImportUndoResult> Undo(ImportSession session, string userId, CancellationToken cancellationToken = default)
    {
        var entries = await ImportSessions.GetEntries(session.Id, cancellationToken);
        var reverted = 0;
        var blocked = new List<string>();

        foreach (var entry in entries)
        {
            var handler = Catalog.Resolve(entry.EntityType);

            if (handler is null)
            {
                blocked.Add($"'{entry.EntityType}' records cannot be undone.");
                continue;
            }

            var context = new EntityUndoContext
            {
                WorkspaceId = session.WorkspaceId,
                UserId = userId,
                EntityId = entry.EntityId,
                PreviousValues = entry.PreviousValues,
                ExpectedUpdatedAt = entry.EntityUpdatedAt,
            };
            var result = entry.Operation == ImportEntryOperation.Created
                ? await handler.RevertCreate(context, cancellationToken)
                : await handler.RevertUpdate(context, cancellationToken);

            if (result.IsSuccess)
            {
                reverted++;
                continue;
            }

            blocked.Add(result.Reason ?? $"Entry {entry.Id} could not be undone.");
        }

        // Only move off Committed when something actually came back. Undo is allowed from Committed
        // alone, so marking a run that reverted nothing as Undone would leave no way to retry it.
        if (reverted > 0)
        {
            session.Stage = ImportStage.Undone;
            session.ProgressMessage = blocked.Count == 0 ? "Undone" : $"Undone, {blocked.Count} left in place";

            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        return new ImportUndoResult
        {
            Reverted = reverted,
            Blocked = blocked,
        };
    }
}
