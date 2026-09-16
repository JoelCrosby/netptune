using Mediator;

using Netptune.Core.Requests;
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
        var ids = request.Ids.ToList();
        var overflow = RequestLimits.DescribeBulkIdOverflow(ids.Count);

        if (overflow is not null)
        {
            return ClientResponse.Failed(overflow);
        }

        var userId = Identity.GetCurrentUserId();

        await UnitOfWork.Notifications.SoftDelete(ids, userId, cancellationToken);

        return ClientResponse.Success;
    }
}
