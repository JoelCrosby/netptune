using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Boards.Commands.DeleteBoard;

public sealed class DeleteBoardCommandHandler : IRequestHandler<DeleteBoardCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteBoardCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteBoardCommand request, CancellationToken cancellationToken)
    {
        var board = await UnitOfWork.Boards.GetAsync(request.Id);

        if (board is null) return ClientResponse.NotFound;

        var userId = Identity.GetCurrentUserId();

        board.Delete(userId);

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = board.Id;
            options.EntityType = EntityType.Board;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
