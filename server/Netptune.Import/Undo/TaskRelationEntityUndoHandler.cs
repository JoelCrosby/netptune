using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Undo;

namespace Netptune.Import.Undo;

public sealed class TaskRelationEntityUndoHandler : IEntityUndoHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public TaskRelationEntityUndoHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public string EntityType => ImportEntryTypes.TaskRelation;

    public async Task<EntityUndoResult> RevertCreate(EntityUndoContext context, CancellationToken cancellationToken = default)
    {
        var relation = await UnitOfWork.ProjectTaskRelations.GetInWorkspace(
            context.EntityId,
            context.WorkspaceId,
            cancellationToken);

        if (relation is null)
        {
            return EntityUndoResult.Success;
        }

        await UnitOfWork.ProjectTaskRelations.DeletePermanent(relation.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return EntityUndoResult.Success;
    }

    public Task<EntityUndoResult> RevertUpdate(EntityUndoContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EntityUndoResult.Success);
    }
}
