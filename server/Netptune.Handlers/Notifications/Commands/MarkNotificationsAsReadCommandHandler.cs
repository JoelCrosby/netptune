using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Notifications.Commands;

public sealed record MarkNotificationsAsReadCommand(IEnumerable<int> Ids) : IRequest<ClientResponse>;

public sealed class MarkNotificationsAsReadCommandHandler : IRequestHandler<MarkNotificationsAsReadCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public MarkNotificationsAsReadCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(MarkNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();

        await UnitOfWork.Notifications.MarkAsRead(request.Ids, userId, cancellationToken);

        return ClientResponse.Success;
    }
}
