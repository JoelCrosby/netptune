using Mediator;

using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record MoveTasksToBoardGroupCommand(List<int> TaskIds, int BoardGroupId) : IRequest<ClientResponse>;

public sealed class MoveTasksToBoardGroupCommandHandler : IRequestHandler<MoveTasksToBoardGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IMediator Mediator;
    private readonly ITaskPlacementService Placement;

    public MoveTasksToBoardGroupCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IMediator mediator,
        ITaskPlacementService placement)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Mediator = mediator;
        Placement = placement;
    }

    public async ValueTask<ClientResponse> Handle(
        MoveTasksToBoardGroupCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var target = await UnitOfWork.BoardGroups.GetTaskTarget(request.BoardGroupId, cancellationToken);
        var isInWorkspace = target is not null && target.WorkspaceId == workspaceId;

        if (!isInWorkspace)
        {
            return ClientResponse.NotFound;
        }

        var hasBoard = !string.IsNullOrWhiteSpace(target!.BoardIdentifier);

        if (!hasBoard)
        {
            return ClientResponse.Failed("The board group does not belong to a board.");
        }

        var tasks = await UnitOfWork.Tasks.GetAllByIdAsync(request.TaskIds, true, cancellationToken);
        var isMissingTask = tasks.Count != request.TaskIds.Distinct().Count();

        if (isMissingTask)
        {
            return ClientResponse.NotFound;
        }

        var isOutsideBoardProject = tasks.Any(task => task.ProjectId != target.ProjectId);

        if (isOutsideBoardProject)
        {
            return ClientResponse.Failed($"Board group “{target.Name}” belongs to a different project.");
        }

        await EnsureOnBoard(request.TaskIds, target, cancellationToken);

        var moveRequest = new MoveTasksToGroupRequest
        {
            BoardId = target.BoardIdentifier!,
            TaskIds = request.TaskIds,
            NewGroupId = target.Id,
        };

        return await Mediator.Send(new MoveTasksToGroupCommand(moveRequest), cancellationToken);
    }

    private async Task EnsureOnBoard(
        List<int> taskIds,
        BoardGroupTaskTarget target,
        CancellationToken cancellationToken)
    {
        var taskIdsInBoard = await UnitOfWork.Tasks.GetTaskIdsInBoard(target.BoardIdentifier!, cancellationToken);
        var missingTaskIds = taskIds.Distinct().Where(taskId => !taskIdsInBoard.Contains(taskId)).ToList();

        if (missingTaskIds.Count == 0)
        {
            return;
        }

        await Placement.PlaceMany(missingTaskIds, target, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);
    }
}
