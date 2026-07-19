using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Events;
using Netptune.Core.Ordering;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
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

    public MoveTaskInBoardGroupCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IIdentityService identity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        EventPublisher = eventPublisher;
        Identity = identity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse> Handle(MoveTaskInBoardGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var response = req.OldGroupId == req.NewGroupId
            ? await MoveTaskInGroup(req, cancellationToken)
            : await TransferTaskInGroups(req, cancellationToken);

        return response;
    }

    private async Task<ClientResponse> TransferTaskInGroups(MoveTaskInGroupRequest request, CancellationToken cancellationToken)
    {
        var oldTask = await UnitOfWork.Tasks.GetTaskViewModel(request.TaskId, cancellationToken);

        var boardGroup = await UnitOfWork.Transaction(async () =>
        {
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
                    }
                }
            }

            await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId(new[] { request.TaskId }, cancellationToken);

            var newRelational = new ProjectTaskInBoardGroup
            {
                ProjectTaskId = request.TaskId,
                BoardGroupId = request.NewGroupId,
                SortOrder = -1,
            };

            await UnitOfWork.ProjectTasksInGroups.AddAsync(newRelational, cancellationToken);
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

        Activity.LogWith<MoveTaskActivityMeta>(options =>
        {
            options.EntityId = request.TaskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta { Group = boardGroup.Name, GroupId = boardGroup.Id };
        });

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
