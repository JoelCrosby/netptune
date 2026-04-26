using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Notifications.Commands.MarkAllAsRead;

public sealed class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public MarkAllAsReadCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();

        await UnitOfWork.Notifications.MarkAllAsRead(userId, workspaceId);

        return ClientResponse.Success;
    }
}
