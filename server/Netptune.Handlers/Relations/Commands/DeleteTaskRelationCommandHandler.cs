using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Relations;

namespace Netptune.Handlers.Relations.Commands;

public sealed record DeleteTaskRelationCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteTaskRelationCommandHandler : IRequestHandler<DeleteTaskRelationCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteTaskRelationCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTaskRelationCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse.NotFound;

        var relation = await UnitOfWork.ProjectTaskRelations.GetInWorkspace(request.Id, workspaceId.Value, cancellationToken);

        if (relation is null) return ClientResponse.NotFound;

        // Read both sides before the row goes, since the activity entries describe what was removed
        // and there is nothing left to describe it with afterwards.
        var sourceView = await GetView(relation.Id, relation.SourceTaskId, workspaceId.Value, cancellationToken);
        var targetView = await GetView(relation.Id, relation.TargetTaskId, workspaceId.Value, cancellationToken);

        // Links are hard-deleted, matching how task tags behave — there is nothing to restore.
        await UnitOfWork.ProjectTaskRelations.DeletePermanent(relation.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        if (sourceView is not null) LogRelation(relation.SourceTaskId, sourceView);
        if (targetView is not null) LogRelation(relation.TargetTaskId, targetView);

        return ClientResponse.Success;
    }

    private async Task<TaskRelationViewModel?> GetView(int relationId, int taskId, int workspaceId, CancellationToken cancellationToken)
    {
        var relations = await UnitOfWork.ProjectTaskRelations.GetRelationsForTask(taskId, workspaceId, cancellationToken);

        return relations.FirstOrDefault(relation => relation.Id == relationId);
    }

    private void LogRelation(int taskId, TaskRelationViewModel view)
    {
        Activity.LogWith<TaskRelationActivityMeta>(options =>
        {
            options.EntityId = taskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.RemoveRelation;
            options.Meta = new TaskRelationActivityMeta
            {
                RelationTypeId = view.RelationTypeId,
                RelationTypeName = view.RelationTypeName,
                Label = view.Label,
                RelatedTaskId = view.RelatedTask.Id,
                RelatedTaskSystemId = view.RelatedTask.SystemId,
                RelatedTaskName = view.RelatedTask.Name,
            };
        });
    }
}
