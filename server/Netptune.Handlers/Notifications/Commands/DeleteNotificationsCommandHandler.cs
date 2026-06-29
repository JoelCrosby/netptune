using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Notifications.Commands;

public sealed record DeleteNotificationsCommand(IEnumerable<int> Ids) : IRequest<ClientResponse>;

public sealed class DeleteNotificationsCommandHandler : IRequestHandler<DeleteNotificationsCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DeleteNotificationsCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteNotificationsCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();

        await UnitOfWork.Notifications.SoftDelete(request.Ids, userId, cancellationToken);

        return ClientResponse.Success;
    }
}
