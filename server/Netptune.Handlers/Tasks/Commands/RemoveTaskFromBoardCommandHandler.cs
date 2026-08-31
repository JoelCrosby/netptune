using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record RemoveTaskFromBoardCommand(int TaskId, int BoardId) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class RemoveTaskFromBoardCommandHandler : IRequestHandler<RemoveTaskFromBoardCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly ITaskPlacementService Placement;

    public RemoveTaskFromBoardCommandHandler(
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

    public async ValueTask<ClientResponse<TaskViewModel>> Handle(RemoveTaskFromBoardCommand request, CancellationToken cancellationToken)
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

        var placement = await UnitOfWork.ProjectTasksInGroups.GetPlacementOnBoard(task.Id, board.Id, cancellationToken);
        var removed = await Placement.RemoveFromBoard(task.Id, board.Id, cancellationToken);

        if (!removed)
        {
            return ClientResponse<TaskViewModel>.Failed($"The task is not on board '{board.Name}'");
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogWith<RemoveTaskFromBoardActivityMeta>(options =>
        {
            options.EntityId = task.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Remove;
            options.Meta = new RemoveTaskFromBoardActivityMeta
            {
                Board = board.Name,
                BoardId = board.Id,
                GroupId = placement?.BoardGroupId,
            };
        });

        var response = await UnitOfWork.Tasks.GetTaskViewModel(task.Id, cancellationToken);

        return ClientResponse<TaskViewModel>.Success(response!);
    }
}
