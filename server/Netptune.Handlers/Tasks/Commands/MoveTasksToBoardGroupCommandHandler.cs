using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record MoveTasksToBoardGroupCommand(List<int> TaskIds, int BoardGroupId) : IRequest<ClientResponse>;

public sealed class MoveTasksToBoardGroupCommandHandler : IRequestHandler<MoveTasksToBoardGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IMediator Mediator;

    public MoveTasksToBoardGroupCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IMediator mediator)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Mediator = mediator;
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

        var moveRequest = new MoveTasksToGroupRequest
        {
            BoardId = target.BoardIdentifier!,
            TaskIds = request.TaskIds,
            NewGroupId = target.Id,
        };

        return await Mediator.Send(new MoveTasksToGroupCommand(moveRequest), cancellationToken);
    }
}
