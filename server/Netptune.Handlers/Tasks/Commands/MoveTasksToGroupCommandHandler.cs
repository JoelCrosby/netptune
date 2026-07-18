using Mediator;

using Netptune.Core.Entities;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Events;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record MoveTasksToGroupCommand(MoveTasksToGroupRequest Request) : IRequest<ClientResponse>;

public sealed class MoveTasksToGroupCommandHandler : IRequestHandler<MoveTasksToGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;

    public MoveTasksToGroupCommandHandler(
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

    public async ValueTask<ClientResponse> Handle(MoveTasksToGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var boardGroup = await UnitOfWork.BoardGroups.GetTaskTarget(req.NewGroupId!.Value, cancellationToken);

        if (boardGroup is null)
        {
            return ClientResponse.Failed();
        }

        var taskIdsInBoard = await UnitOfWork.Tasks.GetTaskIdsInBoard(req.BoardId, cancellationToken);
        var taskIds = req.TaskIds.Where(id => taskIdsInBoard.Contains(id)).ToList();

        var oldTasks = boardGroup.StatusId.HasValue
            ? await UnitOfWork.Tasks.GetAllByIdAsync(taskIds, true, cancellationToken)
            : [];

        await UnitOfWork.Transaction(async () =>
        {

            if (boardGroup.StatusId.HasValue)
            {
                await UnitOfWork.Tasks.UpdateTaskStatuses(taskIds, boardGroup.StatusId.Value, cancellationToken);
                var workspaceId = await Identity.GetWorkspaceId();
                var newStatus = await UnitOfWork.Statuses.GetInWorkspace(boardGroup.StatusId.Value, workspaceId, cancellationToken: cancellationToken);

                if (newStatus is not null)
                {
                    foreach (var oldTask in oldTasks.Where(task => task.StatusId != newStatus.Id))
                    {
                        var references = new List<EventReferenceInput>();

                        if (oldTask.ProjectId.HasValue)
                        {
                            references.Add(new(EventReferenceRoles.Scope, EventEntityTypes.From(EntityType.Project), oldTask.ProjectId.Value.ToString()));
                        }

                        if (oldTask.SprintId.HasValue)
                        {
                            references.Add(new(EventReferenceRoles.Scope, EventEntityTypes.From(EntityType.Sprint), oldTask.SprintId.Value.ToString()));
                        }
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
                                OldCategory = oldTask.Status.Category.ToString(),
                                NewCategory = newStatus.Category.ToString(),
                            },
                            References = references,
                        }, cancellationToken);
                    }
                }
            }

            await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId(taskIds, cancellationToken);

            var baseSortOrder = await UnitOfWork.BoardGroups.GetMaxTaskSortOrder(boardGroup.Id, cancellationToken) + 1;

            var taskInGroups = taskIds.Select((id, index) => new ProjectTaskInBoardGroup
            {
                BoardGroupId = boardGroup.Id,
                ProjectTaskId = id,
                SortOrder = baseSortOrder + index,
            });

            await UnitOfWork.ProjectTasksInGroups.AddRangeAsync(taskInGroups, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        Activity.LogWithMany<MoveTaskActivityMeta>(options =>
        {
            options.EntityIds = taskIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta { Group = boardGroup.Name, GroupId = boardGroup.Id };
        });

        if (boardGroup.StatusId.HasValue)
        {
            foreach (var oldTask in oldTasks.Where(task => task.StatusId != boardGroup.StatusId.Value))
            {
                await PublishTaskChanged(
                    oldTask.Id,
                    oldTask.WorkspaceId,
                    oldTask.StatusId,
                    boardGroup.StatusId.Value);
            }
        }

        return ClientResponse.Success;
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
