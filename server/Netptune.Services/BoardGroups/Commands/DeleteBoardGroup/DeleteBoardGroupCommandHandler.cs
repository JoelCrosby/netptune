using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.BoardGroups.Commands;

public sealed class DeleteBoardGroupCommandHandler : IRequestHandler<DeleteBoardGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteBoardGroupCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteBoardGroupCommand request, CancellationToken cancellationToken)
    {
        var boardGroup = await UnitOfWork.BoardGroups.GetAsync(request.Id);

        if (boardGroup is null) return ClientResponse.NotFound;

        var userId = Identity.GetCurrentUserId();

        boardGroup.Delete(userId);

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = boardGroup.Id;
            options.EntityType = EntityType.BoardGroup;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
