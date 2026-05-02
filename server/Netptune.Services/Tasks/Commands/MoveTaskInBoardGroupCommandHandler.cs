using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Ordering;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Tasks.Commands;

public sealed record MoveTaskInBoardGroupCommand(MoveTaskInGroupRequest Request) : IRequest<ClientResponse>;

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
            ? await MoveTaskInGroup(req, cancellationToken)
            : await TransferTaskInGroups(req, cancellationToken);
    }

    private async Task<ClientResponse> TransferTaskInGroups(MoveTaskInGroupRequest request, CancellationToken cancellationToken)
    {
        var boardGroup = await UnitOfWork.Transaction(async () =>
        {
            var itemToRemove = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.OldGroupId, cancellationToken);

            if (itemToRemove is not null)
            {
                await UnitOfWork.ProjectTasksInGroups.DeletePermanent(itemToRemove.Id);
            }

            var tasks = await UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId, cancellationToken: cancellationToken);
            var existing = tasks.Where(x => x.Id == request.TaskId).ToList();

            await UnitOfWork.ProjectTasksInGroups.DeletePermanent(existing.Select(item => item.Id));

            var newGroup = await UnitOfWork.BoardGroups.GetAsync(request.NewGroupId, cancellationToken: cancellationToken);
            var task = await UnitOfWork.Tasks.GetAsync(request.TaskId, cancellationToken: cancellationToken);

            if (newGroup is null || task is null) return null;

            task.Status = newGroup.Type.GetTaskStatusFromGroupType();

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

        var sortOrder = await GetTaskInGroupSortOrder(request.NewGroupId, request.TaskId, request.PreviousIndex, request.CurrentIndex, true, cancellationToken);

        if (boardGroup is null || taskInBoardGroup is null) return ClientResponse.NotFound;

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

        item.SortOrder = await GetTaskInGroupSortOrder(request.NewGroupId, request.TaskId, request.PreviousIndex, request.CurrentIndex, cancellationToken: cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = request.TaskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success;
    }

    private async Task<double> GetTaskInGroupSortOrder(int groupId, int taskId, int previousIndex, int currentIndex, bool isNewItem = false, CancellationToken cancellationToken = default)
    {
        var tasks = await UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(groupId, cancellationToken);
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
