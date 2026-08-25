using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record AddTaskToBoardCommand(int TaskId, int BoardId, int? BoardGroupId) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class AddTaskToBoardCommandHandler : IRequestHandler<AddTaskToBoardCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly ITaskPlacementService Placement;

    public AddTaskToBoardCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        ITaskPlacementService placement)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        Placement = placement;
    }

    public async ValueTask<ClientResponse<TaskViewModel>> Handle(AddTaskToBoardCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var task = await UnitOfWork.Tasks.GetInWorkspace(request.TaskId, workspaceId, cancellationToken: cancellationToken);

        if (task is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        var board = await UnitOfWork.Boards.GetInWorkspace(request.BoardId, workspaceId, cancellationToken: cancellationToken);

        if (board is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        var belongsToTaskProject = task.ProjectId.HasValue && board.ProjectId == task.ProjectId.Value;

        if (!belongsToTaskProject)
        {
            return ClientResponse<TaskViewModel>.Failed($"Board '{board.Name}' does not belong to the task's project");
        }

        var target = await ResolveTarget(request, board.Id, task.StatusId, cancellationToken);

        if (target is null)
        {
            return ClientResponse<TaskViewModel>.Failed($"Board '{board.Name}' has no group to place the task in");
        }

        var placed = await Placement.Place(task.Id, target, cancellationToken);

        if (!placed)
        {
            return ClientResponse<TaskViewModel>.Failed($"The task is already on board '{board.Name}'");
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogWith<MoveTaskActivityMeta>(options =>
        {
            options.EntityId = task.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta { Group = target.Name, GroupId = target.Id };
        });

        var response = await UnitOfWork.Tasks.GetTaskViewModel(task.Id, cancellationToken);

        return ClientResponse<TaskViewModel>.Success(response!);
    }

    private async Task<BoardGroupTaskTarget?> ResolveTarget(
        AddTaskToBoardCommand request,
        int boardId,
        int statusId,
        CancellationToken cancellationToken)
    {
        if (!request.BoardGroupId.HasValue)
        {
            return await Placement.ResolveEntryTarget(boardId, statusId, cancellationToken);
        }

        var requested = await UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId.Value, cancellationToken);
        var belongsToBoard = requested?.BoardId == boardId;

        return belongsToBoard ? requested : null;
    }
}
