using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Localization;
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
            var error = Localized.Format(AppText.ErrorBoardNotInTaskProject, board.Name);

            return ClientResponse<TaskViewModel>.Failed(error);
        }

        var target = await ResolveTarget(request, board.Id, task.StatusId, cancellationToken);

        if (target is null)
        {
            var error = Localized.Format(AppText.ErrorBoardNoGroupPlaceTask, board.Name);

            return ClientResponse<TaskViewModel>.Failed(error);
        }

        var currentPlacement = await UnitOfWork.ProjectTasksInGroups.GetPlacementOnBoard(task.Id, board.Id, cancellationToken);
        var placed = await Placement.Place(task.Id, target, cancellationToken);

        if (!placed)
        {
            return ClientResponse<TaskViewModel>.Failed(Localized.Format(AppText.ErrorTaskAlreadyBoard, board.Name));
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        // A task already on the board is only changing column, so the board is reported as joined
        // only when it had no placement there before.
        var joinedBoardId = currentPlacement is null ? board.Id : (int?)null;

        Activity.LogWith<MoveTaskActivityMeta>(options =>
        {
            options.EntityId = task.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta
            {
                Group = target.Name,
                GroupId = target.Id,
                BoardId = joinedBoardId,
                FromGroupId = currentPlacement?.BoardGroupId,
            };
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
