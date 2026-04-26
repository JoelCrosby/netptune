using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Tasks.Commands.MoveTasksToGroup;

public sealed class MoveTasksToGroupCommandHandler : IRequestHandler<MoveTasksToGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public MoveTasksToGroupCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(MoveTasksToGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var boardGroup = await UnitOfWork.BoardGroups.GetAsync(req.NewGroupId!.Value);

        if (boardGroup is null) return ClientResponse.Failed();

        var taskIdsInBoard = await UnitOfWork.Tasks.GetTaskIdsInBoard(req.BoardId);
        var taskIds = req.TaskIds.Where(id => taskIdsInBoard.Contains(id)).ToList();

        var ids = await UnitOfWork.ProjectTasksInGroups.GetAllByTaskId(taskIds);
        await UnitOfWork.ProjectTasksInGroups.DeletePermanent(ids);

        var baseSortOrder = boardGroup.TasksInGroups
            .OrderByDescending(task => task.SortOrder)
            .Select(task => task.SortOrder)
            .FirstOrDefault() + 1;

        var taskInGroups = taskIds.Select((id, index) => new ProjectTaskInBoardGroup
        {
            BoardGroupId = boardGroup.Id,
            ProjectTaskId = id,
            SortOrder = baseSortOrder + index,
        });

        await UnitOfWork.ProjectTasksInGroups.AddRangeAsync(taskInGroups);
        await UnitOfWork.CompleteAsync();

        Activity.LogWithMany<MoveTaskActivityMeta>(options =>
        {
            options.EntityIds = taskIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta { Group = boardGroup.Name, GroupId = boardGroup.Id };
        });

        return ClientResponse.Success;
    }
}
