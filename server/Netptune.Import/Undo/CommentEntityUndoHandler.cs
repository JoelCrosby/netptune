using Netptune.Core.Events;
using Netptune.Core.Localization;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Undo;

namespace Netptune.Import.Undo;

public sealed class CommentEntityUndoHandler : IEntityUndoHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public CommentEntityUndoHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public string EntityType => EventEntityTypes.From(Core.Enums.EntityType.Comment);

    public async Task<EntityUndoResult> RevertCreate(EntityUndoContext context, CancellationToken cancellationToken = default)
    {
        var comment = await UnitOfWork.Comments.GetCommentForUpdate(context.EntityId, context.WorkspaceId, cancellationToken);

        if (comment is null)
        {
            return EntityUndoResult.Success;
        }

        var wasEditedSinceImport = HasBeenModifiedSince(comment.UpdatedAt, context.ExpectedUpdatedAt);

        if (wasEditedSinceImport)
        {
            return EntityUndoResult.Blocked(Localized.Format(AppText.ImportUndoCommentChanged, comment.Id));
        }

        await UnitOfWork.Comments.DeletePermanent(comment.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return EntityUndoResult.Success;
    }

    public Task<EntityUndoResult> RevertUpdate(EntityUndoContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EntityUndoResult.Success);
    }

    private static bool HasBeenModifiedSince(DateTime? actual, DateTime? expected)
    {
        if (expected is null || actual is null)
        {
            return false;
        }

        return actual.Value > expected.Value.AddSeconds(1);
    }
}
