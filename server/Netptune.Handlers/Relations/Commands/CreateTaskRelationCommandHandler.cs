using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.Relations;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Relations;

namespace Netptune.Handlers.Relations.Commands;

public sealed record CreateTaskRelationCommand(CreateTaskRelationRequest Request) : IRequest<ClientResponse<TaskRelationViewModel>>;

public sealed class CreateTaskRelationCommandHandler : IRequestHandler<CreateTaskRelationCommand, ClientResponse<TaskRelationViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public CreateTaskRelationCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse<TaskRelationViewModel>> Handle(CreateTaskRelationCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse<TaskRelationViewModel>.NotFound;

        var relationType = await UnitOfWork.RelationTypes.GetInWorkspace(request.Request.RelationTypeId, workspaceId.Value, true, cancellationToken);

        if (relationType is null) return ClientResponse<TaskRelationViewModel>.NotFound;

        // Resolving through the workspace key is what keeps this from linking across workspaces — a
        // system id belonging to another workspace simply will not resolve. Both tasks are fetched in
        // full because the response and the activity entries need to name them.
        var requestingTask = await UnitOfWork.Tasks.GetTaskViewModel(request.Request.SourceSystemId, workspaceKey, cancellationToken);
        var otherTask = await UnitOfWork.Tasks.GetTaskViewModel(request.Request.TargetSystemId, workspaceKey, cancellationToken);

        if (requestingTask is null || otherTask is null)
        {
            return ClientResponse<TaskRelationViewModel>.NotFound;
        }

        if (requestingTask.Id == otherTask.Id)
        {
            return ClientResponse<TaskRelationViewModel>.Failed("A task cannot be related to itself.");
        }

        var (source, target) = RelationTypeRules.Orient(relationType.Category, requestingTask.Id, otherTask.Id);
        var requestingIsSource = source == requestingTask.Id;

        ProjectTaskRelation? committedRelation = null;

        var response = await UnitOfWork.Transaction(async () =>
        {
            if (await UnitOfWork.ProjectTaskRelations.Exists(relationType.Id, source, target, cancellationToken))
            {
                return ClientResponse<TaskRelationViewModel>.Failed("These tasks are already linked by this relation.");
            }

            if (RelationTypeRules.HasSingleSource(relationType.Category) &&
                await UnitOfWork.ProjectTaskRelations.HasExistingSource(relationType.Id, target, cancellationToken))
            {
                return ClientResponse<TaskRelationViewModel>.Failed(
                    $"That task already has a \"{relationType.Name}\" link. A task can only have one.");
            }

            if (RelationTypeRules.IsAcyclic(relationType.Category) &&
                await UnitOfWork.ProjectTaskRelations.WouldCreateCycle(relationType.Id, source, target, cancellationToken))
            {
                return ClientResponse<TaskRelationViewModel>.Failed(
                    $"This would create a circular \"{relationType.Name}\" chain.");
            }

            var relation = new ProjectTaskRelation
            {
                WorkspaceId = workspaceId.Value,
                RelationTypeId = relationType.Id,
                SourceTaskId = source,
                TargetTaskId = target,
            };

            await UnitOfWork.ProjectTaskRelations.AddAsync(relation, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            committedRelation = relation;

            var requestingView = TaskRelationViewModel.BuildView(relation.Id, relationType, requestingIsSource, otherTask);
            var otherView = TaskRelationViewModel.BuildView(relation.Id, relationType, !requestingIsSource, requestingTask);

            LogRelation(requestingTask.Id, requestingView);
            LogRelation(otherTask.Id, otherView);

            return ClientResponse<TaskRelationViewModel>.Success(requestingView);
        });

        if (committedRelation is not null)
        {
            await PublishRelationChanged(committedRelation, relationType.Category, TaskRelationChange.Added);
        }

        return response;
    }

    private async Task PublishRelationChanged(
        ProjectTaskRelation relation,
        RelationCategory category,
        TaskRelationChange change)
    {
        var user = await Identity.GetCurrentUser();

        await EventPublisher.Dispatch(new TaskRelationChangedMessage
        {
            WorkspaceId = relation.WorkspaceId,
            SourceTaskId = relation.SourceTaskId,
            TargetTaskId = relation.TargetTaskId,
            Category = category,
            Change = change,
            ActorUserId = user.Id,
        });
    }

    private void LogRelation(int taskId, TaskRelationViewModel view)
    {
        Activity.LogWith<TaskRelationActivityMeta>(options =>
        {
            options.EntityId = taskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.AddRelation;
            options.Meta = TaskRelationActivityMeta.From(view);
        });
    }
}
