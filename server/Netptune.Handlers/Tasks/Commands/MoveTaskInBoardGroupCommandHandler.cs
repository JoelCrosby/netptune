using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Ordering;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record MoveTaskInBoardGroupCommand(MoveTaskInGroupRequest Request) : IRequest<ClientResponse>;

public sealed class MoveTaskInBoardGroupCommandHandler : IRequestHandler<MoveTaskInBoardGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public MoveTaskInBoardGroupCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(MoveTaskInBoardGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        return req.OldGroupId == req.NewGroupId
            ? await MoveTaskInGroup(req, cancellationToken)
            : await TransferTaskInGroups(req, cancellationToken);
    }

    private async Task<ClientResponse> TransferTaskInGroups(MoveTaskInGroupRequest request, CancellationToken cancellationToken)
    {
        var boardGroup = await UnitOfWork.Transaction(async () =>
        {
            var newGroup = await UnitOfWork.BoardGroups.GetTaskTarget(request.NewGroupId, cancellationToken);

            if (newGroup is null) return null;

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

        if (boardGroup is null || taskInBoardGroup is null) return ClientResponse.NotFound;

        var sortOrder = await GetTaskInGroupSortOrder(request.NewGroupId, request.TaskId, request.CurrentIndex, true, cancellationToken);

        taskInBoardGroup.SortOrder = sortOrder;

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogWith<MoveTaskActivityMeta>(options =>
        {
            options.EntityId = request.TaskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta { Group = boardGroup.Name, GroupId = boardGroup.Id };
        });

        return ClientResponse.Success;
    }

    private async Task<ClientResponse> MoveTaskInGroup(MoveTaskInGroupRequest request, CancellationToken cancellationToken)
    {
        var item = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, cancellationToken);

        if (item is null) return ClientResponse.NotFound;

        item.SortOrder = await GetTaskInGroupSortOrder(request.NewGroupId, request.TaskId, request.CurrentIndex, cancellationToken: cancellationToken);

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

            if (item is null) throw new($"Task with id of {taskId} does not exist in group {groupId}.");
        }

        var (preOrder, nextOrder) = await UnitOfWork.ProjectTasksInGroups.GetNeighborSortOrdersForInsert(
            groupId,
            taskId,
            currentIndex,
            cancellationToken);

        return OrderingUtils.GetNewSortOrder(preOrder, nextOrder);
    }
}
