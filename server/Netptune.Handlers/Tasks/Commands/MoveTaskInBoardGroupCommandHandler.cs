using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Ordering;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record MoveTaskInBoardGroupCommand(MoveTaskInGroupRequest Request) : IRequest<ClientResponse>;

public sealed class MoveTaskInBoardGroupCommandHandler : IRequestHandler<MoveTaskInBoardGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;
    private readonly ITaskPlacementService Placement;

    public MoveTaskInBoardGroupCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IIdentityService identity,
        IEventRecordWriter eventRecords,
        ITaskPlacementService placement)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        EventPublisher = eventPublisher;
        Identity = identity;
        EventRecords = eventRecords;
        Placement = placement;
    }

    public async ValueTask<ClientResponse> Handle(MoveTaskInBoardGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var workspaceId = await Identity.GetWorkspaceId();
        var task = await UnitOfWork.Tasks.GetInWorkspace(req.TaskId, workspaceId, true, cancellationToken);
        var newGroup = await UnitOfWork.BoardGroups.GetInWorkspace(req.NewGroupId, workspaceId, true, cancellationToken);
        var isInWorkspace = task is not null && newGroup is not null;

        if (!isInWorkspace)
        {
            return ClientResponse.NotFound;
        }

        var response = req.OldGroupId == req.NewGroupId
            ? await MoveTaskInGroup(req, cancellationToken)
            : await TransferTaskInGroups(req, workspaceId, cancellationToken);

        return response;
    }

    private async Task<ClientResponse> TransferTaskInGroups(
        MoveTaskInGroupRequest request,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var oldTask = await UnitOfWork.Tasks.GetTaskViewModel(request.TaskId, cancellationToken);
        var moveContext = new TaskBoardMoveContext
        {
            WorkspaceId = workspaceId,
            CorrelationId = Guid.NewGuid(),
        };

        var boardGroup = await UnitOfWork.Transaction(async () =>
        {
            var statusMoves = TaskBoardMoves.None;
            var newGroup = await UnitOfWork.BoardGroups.GetTaskTarget(request.NewGroupId, cancellationToken);

            if (newGroup is null)
            {
                return null;
            }

            if (newGroup.StatusId.HasValue)
            {
                await UnitOfWork.Tasks.UpdateTaskStatus(request.TaskId, newGroup.StatusId.Value, cancellationToken);

                if (oldTask is not null && oldTask.StatusId != newGroup.StatusId.Value)
                {
                    var newStatus = await UnitOfWork.Statuses.GetInWorkspace(newGroup.StatusId.Value, oldTask.WorkspaceId!.Value, cancellationToken: cancellationToken);

                    if (newStatus is not null)
                    {
                        await EventRecords.Append(new EventWriteRequest<FieldTransitionedPayload>
                        {
                            WorkspaceId = oldTask.WorkspaceId,
                            EventKey = EventKeys.EntityFieldTransitioned,
                            SubjectType = EventEntityTypes.From(EntityType.Task),
                            SubjectId = oldTask.Id.ToString(),
                            CorrelationId = moveContext.CorrelationId,
                            Payload = new FieldTransitionedPayload
                            {
                                Field = "status",
                                OldValue = oldTask.StatusId.ToString(),
                                NewValue = newStatus.Id.ToString(),
                                OldCategory = oldTask.StatusCategory.ToString(),
                                NewCategory = newStatus.Category.ToString(),
                            },
                            References = BuildReferences(oldTask.ProjectId, oldTask.SprintId),
                        }, cancellationToken);

                        statusMoves = await Placement.FollowStatus(
                            [request.TaskId],
                            newStatus,
                            newGroup.BoardId,
                            cancellationToken);
                    }
                }
            }

            var placed = await Placement.Place(request.TaskId, newGroup, cancellationToken);

            if (placed)
            {
                var move = new TaskBoardMove(request.TaskId, request.OldGroupId, newGroup);

                await Placement.RecordMoves(new TaskBoardMoves(null, [move]), moveContext, cancellationToken);
            }

            await Placement.RecordMoves(statusMoves, moveContext, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            return newGroup;
        });

        var taskInBoardGroup = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, cancellationToken);

        if (boardGroup is null || taskInBoardGroup is null)
        {
            return ClientResponse.NotFound;
        }

        var sortOrder = await GetTaskInGroupSortOrder(
            request.NewGroupId,
            request.TaskId,
            request.CurrentIndex,
            true,
            cancellationToken);

        taskInBoardGroup.SortOrder = sortOrder;

        await UnitOfWork.CompleteAsync(cancellationToken);

        if (boardGroup.StatusId.HasValue
            && oldTask is not null
            && oldTask.StatusId != boardGroup.StatusId.Value
            && oldTask.WorkspaceId is not null)
        {
            await PublishTaskChanged(
                oldTask.Id,
                oldTask.WorkspaceId.Value,
                oldTask.StatusId,
                boardGroup.StatusId.Value);

            await EventPublisher.IndexTasks([oldTask.Id], oldTask.WorkspaceKey);
        }

        return ClientResponse.Success;
    }

    private static IReadOnlyCollection<EventReferenceInput> BuildReferences(int? projectId, int? sprintId)
    {
        var references = new List<EventReferenceInput>();

        if (projectId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Project),
                EntityId = projectId.Value.ToString(),
            });
        }

        if (sprintId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Sprint),
                EntityId = sprintId.Value.ToString(),
            });
        }

        return references;
    }

    private async Task<ClientResponse> MoveTaskInGroup(MoveTaskInGroupRequest request, CancellationToken cancellationToken)
    {
        var item = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, cancellationToken);

        if (item is null)
        {
            return ClientResponse.NotFound;
        }

        item.SortOrder = await GetTaskInGroupSortOrder(
            request.NewGroupId,
            request.TaskId,
            request.CurrentIndex,
            cancellationToken: cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = request.TaskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success;
    }

    private async Task<double> GetTaskInGroupSortOrder(int groupId, int taskId, int currentIndex, bool isNewItem = false, CancellationToken cancellationToken = default)
    {

        if (!isNewItem)
        {
            var item = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(taskId, groupId, cancellationToken);

            if (item is null)
            {
                throw new($"Task with id of {taskId} does not exist in group {groupId}.");
            }
        }

        var (preOrder, nextOrder) = await UnitOfWork.ProjectTasksInGroups.GetNeighborSortOrdersForInsert(
            groupId,
            taskId,
            currentIndex,
            cancellationToken);

        return OrderingUtils.GetNewSortOrder(preOrder, nextOrder);
    }

    private Task PublishTaskChanged(
        int taskId,
        int workspaceId,
        int oldStatusId,
        int newStatusId)
    {
        return EventPublisher.Dispatch(new TaskChangedMessage
        {
            WorkspaceId = workspaceId,
            TaskId = taskId,
            ActorUserId = Identity.GetCurrentUserId(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, oldStatusId, newStatusId),
            ],
        });
    }
}
