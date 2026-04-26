using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Notifications.Commands.MarkAsRead;

public sealed class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public MarkAsReadCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var notification = await UnitOfWork.Notifications.GetAsync(request.Id);

        if (notification is null || notification.UserId != userId)
        {
            return ClientResponse.NotFound;
        }

        notification.IsRead = true;

        await UnitOfWork.CompleteAsync();

        return ClientResponse.Success;
    }
}
