using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Ordering;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Tasks.Commands;

public sealed class MoveTaskInBoardGroupCommandHandler : IRequestHandler<MoveTaskInBoardGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public MoveTaskInBoardGroupCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(MoveTaskInBoardGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        return req.OldGroupId == req.NewGroupId
            ? await MoveTaskInGroup(req)
            : await TransferTaskInGroups(req);
    }

    private async Task<ClientResponse> TransferTaskInGroups(Core.Requests.MoveTaskInGroupRequest request)
    {
        var boardGroup = await UnitOfWork.Transaction(async () =>
        {
            var itemToRemove = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.OldGroupId);

            if (itemToRemove is not null)
            {
                await UnitOfWork.ProjectTasksInGroups.DeletePermanent(itemToRemove.Id);
            }

            var tasks = await UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId);
            var existing = tasks.Where(x => x.Id == request.TaskId).ToList();

            await UnitOfWork.ProjectTasksInGroups.DeletePermanent(existing.Select(item => item.Id));

            var newGroup = await UnitOfWork.BoardGroups.GetAsync(request.NewGroupId);
            var task = await UnitOfWork.Tasks.GetAsync(request.TaskId);

            if (newGroup is null || task is null) return null;

            task.Status = newGroup.Type.GetTaskStatusFromGroupType();

            var newRelational = new ProjectTaskInBoardGroup
            {
                ProjectTaskId = request.TaskId,
                BoardGroupId = request.NewGroupId,
                SortOrder = -1,
            };

            await UnitOfWork.ProjectTasksInGroups.AddAsync(newRelational);
            await UnitOfWork.CompleteAsync();

            return newGroup;
        });

        var taskInBoardGroup = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId);

        var sortOrder = await GetTaskInGroupSortOrder(request.NewGroupId, request.TaskId, request.PreviousIndex, request.CurrentIndex, true);

        if (boardGroup is null || taskInBoardGroup is null) return ClientResponse.NotFound;

        taskInBoardGroup.SortOrder = sortOrder;

        await UnitOfWork.CompleteAsync();

        Activity.LogWith<MoveTaskActivityMeta>(options =>
        {
            options.EntityId = request.TaskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta { Group = boardGroup.Name, GroupId = boardGroup.Id };
        });

        return ClientResponse.Success;
    }

    private async Task<ClientResponse> MoveTaskInGroup(Core.Requests.MoveTaskInGroupRequest request)
    {
        var item = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId);

        if (item is null) return ClientResponse.NotFound;

        item.SortOrder = await GetTaskInGroupSortOrder(request.NewGroupId, request.TaskId, request.PreviousIndex, request.CurrentIndex);

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = request.TaskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success;
    }

    private async Task<double> GetTaskInGroupSortOrder(int groupId, int taskId, int previousIndex, int currentIndex, bool isNewItem = false)
    {
        var tasks = await UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(groupId);
        var item = tasks.Find(task => task.ProjectTaskId == taskId);

        if (item is null) throw new($"Task with id of {taskId} does not exist in group {groupId}.");
        if (currentIndex < 0 || currentIndex > tasks.Count) throw new($"Get task in group sort order request '{nameof(currentIndex)}' is outside range of board group");

        tasks.RemoveAt(!isNewItem ? previousIndex : 0);
        tasks.Insert(currentIndex, item);

        var preOrder = tasks.ElementAtOrDefault(currentIndex - 1)?.SortOrder;
        var nextOrder = tasks.ElementAtOrDefault(currentIndex + 1)?.SortOrder;

        return OrderingUtils.GetNewSortOrder(preOrder, nextOrder);
    }
}
