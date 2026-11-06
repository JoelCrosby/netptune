using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Localization;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record MoveTasksToGroupCommand(MoveTasksToGroupRequest Request) : IRequest<ClientResponse>;

public sealed class MoveTasksToGroupCommandHandler : IRequestHandler<MoveTasksToGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;
    private readonly ITaskPlacementService Placement;

    public MoveTasksToGroupCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IIdentityService identity,
        IEventRecordWriter eventRecords,
        ITaskPlacementService placement)
    {
        UnitOfWork = unitOfWork;
        EventPublisher = eventPublisher;
        Identity = identity;
        EventRecords = eventRecords;
        Placement = placement;
    }

    public async ValueTask<ClientResponse> Handle(MoveTasksToGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var workspaceId = await Identity.GetWorkspaceId();
        var boardGroup = await UnitOfWork.BoardGroups.GetTaskTarget(req.NewGroupId!.Value, cancellationToken);

        if (boardGroup is null || boardGroup.WorkspaceId != workspaceId)
        {
            return ClientResponse.NotFound;
        }

        var taskIdsInBoard = await UnitOfWork.Tasks.GetTaskIdsInBoard(req.BoardId, workspaceId, cancellationToken);
        var taskIds = req.TaskIds.Distinct().ToList();
        var hasTaskOffBoard = taskIds.Any(id => !taskIdsInBoard.Contains(id));

        if (hasTaskOffBoard)
        {
            return ClientResponse.Failed(AppText.ErrorOnlyTasksAlreadyBoardCanMovedBetween);
        }

        var oldTasks = boardGroup.StatusId.HasValue
            ? await UnitOfWork.Tasks.GetAllByIdAsync(taskIds, true, cancellationToken)
            : [];
        var oldStatusCategories = await GetStatusCategories(oldTasks, cancellationToken);

        // Read before the placement rows are replaced, so the columns the tasks came from survive
        // into the activity these moves log.
        var groupsBeforeMove = await UnitOfWork.ProjectTasksInGroups.GetPlacementGroupsOnBoard(
            taskIds,
            boardGroup.BoardId,
            cancellationToken);

        var moveContext = new TaskBoardMoveContext
        {
            WorkspaceId = workspaceId,
            CorrelationId = Guid.NewGuid(),
        };
        var moves = BuildMoves(taskIds, groupsBeforeMove, boardGroup);

        await UnitOfWork.Transaction(async () =>
        {
            var statusMoves = TaskBoardMoves.None;

            if (boardGroup.StatusId.HasValue)
            {
                await UnitOfWork.Tasks.UpdateTaskStatuses(taskIds, boardGroup.StatusId.Value, cancellationToken);
                var newStatus = await UnitOfWork.Statuses.GetInWorkspace(boardGroup.StatusId.Value, workspaceId, cancellationToken: cancellationToken);

                if (newStatus is not null)
                {
                    var statusChange = new StatusChange(newStatus, oldStatusCategories, moveContext.CorrelationId);
                    var changedTasks = oldTasks.Where(task => task.StatusId != newStatus.Id).ToList();
                    var changedTaskIds = changedTasks.Select(task => task.Id).ToList();
                    var transitions = changedTasks
                        .Select(task => BuildStatusTransition(task, statusChange))
                        .ToList();

                    await EventRecords.AppendRange(transitions, cancellationToken);

                    statusMoves = await Placement.FollowStatus(
                        changedTaskIds,
                        newStatus,
                        boardGroup.BoardId,
                        cancellationToken);
                }
            }

            await Placement.PlaceMany(taskIds, boardGroup, cancellationToken);
            await Placement.RecordMoves(moves, moveContext, cancellationToken);
            await Placement.RecordMoves(statusMoves, moveContext, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
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

            var workspaceKey = Identity.GetWorkspaceKey();

            await EventPublisher.IndexTasks(taskIds, workspaceKey);
        }

        return ClientResponse.Success;
    }

    private async Task<Dictionary<int, string>> GetStatusCategories(
        List<ProjectTask> tasks,
        CancellationToken cancellationToken)
    {
        if (tasks.Count == 0)
        {
            return [];
        }

        var statusIds = tasks.Select(task => task.StatusId).Distinct();
        var statuses = await UnitOfWork.Statuses.GetAllByIdAsync(statusIds, true, cancellationToken);

        return statuses.ToDictionary(status => status.Id, status => status.Category.ToString());
    }

    private sealed record StatusChange(Status NewStatus, Dictionary<int, string> OldStatusCategories, Guid CorrelationId);

    private static EventWriteRequest<FieldTransitionedPayload> BuildStatusTransition(
        ProjectTask oldTask,
        StatusChange change)
    {
        var references = new List<EventReferenceInput>();

        if (oldTask.ProjectId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Project),
                EntityId = oldTask.ProjectId.Value.ToString(),
            });
        }

        if (oldTask.SprintId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Sprint),
                EntityId = oldTask.SprintId.Value.ToString(),
            });
        }

        return new EventWriteRequest<FieldTransitionedPayload>
        {
            WorkspaceId = oldTask.WorkspaceId,
            EventKey = EventKeys.EntityFieldTransitioned,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = oldTask.Id.ToString(),
            CorrelationId = change.CorrelationId,
            Payload = new FieldTransitionedPayload
            {
                Field = "status",
                OldValue = oldTask.StatusId.ToString(),
                NewValue = change.NewStatus.Id.ToString(),
                OldCategory = change.OldStatusCategories.GetValueOrDefault(oldTask.StatusId),
                NewCategory = change.NewStatus.Category.ToString(),
            },
            References = references,
        };
    }

    private static TaskBoardMoves BuildMoves(
        List<int> taskIds,
        Dictionary<int, int> groupsBeforeMove,
        BoardGroupTaskTarget boardGroup)
    {
        var moves = taskIds
            .Select(taskId => new TaskBoardMove(taskId, OriginGroupId(taskId, groupsBeforeMove), boardGroup))
            .ToList();

        return new TaskBoardMoves(null, moves);
    }

    private static int? OriginGroupId(int taskId, Dictionary<int, int> groupsBeforeMove)
    {
        return groupsBeforeMove.TryGetValue(taskId, out var groupId) ? groupId : null;
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
