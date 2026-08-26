using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Boards.Commands;

public sealed record DeleteBoardCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteBoardCommandHandler : IRequestHandler<DeleteBoardCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskPinRepository TaskPins;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteBoardCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        ITaskPinRepository taskPins,
        IIdentityService identity,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        TaskPins = taskPins;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteBoardCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var board = await UnitOfWork.Boards.GetInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (board is null) return ClientResponse.NotFound;

        var userId = Identity.GetCurrentUserId();

        board.Delete(userId);

        var pins = await TaskPins.GetForScopeEntity(workspaceId, TaskPinScope.Board, board.Id, cancellationToken);

        foreach (var pin in pins)
        {
            pin.Delete(userId);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = board.Id;
            options.EntityType = EntityType.Board;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
