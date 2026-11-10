using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Localization;
using Netptune.Core.Models.Search;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Undo;

namespace Netptune.Import.Undo;

// Runs after the import's own tasks are undone, so any task still in the sprint was put there since.
public sealed class SprintEntityUndoHandler : IEntityUndoHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventPublisher EventPublisher;

    public SprintEntityUndoHandler(INetptuneUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        EventPublisher = eventPublisher;
    }

    public string EntityType => EventEntityTypes.From(Core.Enums.EntityType.Sprint);

    public async Task<EntityUndoResult> RevertCreate(EntityUndoContext context, CancellationToken cancellationToken = default)
    {
        var workspace = await UnitOfWork.Workspaces.GetAsync(context.WorkspaceId, cancellationToken: cancellationToken);

        if (workspace is null)
        {
            return EntityUndoResult.Success;
        }

        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(
            workspace.Slug,
            context.EntityId,
            cancellationToken: cancellationToken);

        if (sprint is null || sprint.IsDeleted)
        {
            return EntityUndoResult.Success;
        }

        var isInUse = sprint.ProjectTasks.Any(task => !task.IsDeleted);

        if (isInUse)
        {
            return EntityUndoResult.Blocked(Localized.Format(AppText.ImportUndoSprintInUse, sprint.Name));
        }

        sprint.Delete(context.UserId);

        await UnitOfWork.CompleteAsync(cancellationToken);
        await EventPublisher.RemoveSprints([sprint.Id], workspace.Slug);

        return EntityUndoResult.Success;
    }

    public Task<EntityUndoResult> RevertUpdate(EntityUndoContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EntityUndoResult.Success);
    }
}
