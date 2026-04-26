using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Notifications.Queries;

public sealed class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, ClientResponse<int>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetUnreadCountQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var count = await UnitOfWork.Notifications.GetUnreadCount(userId, workspaceId);

        return ClientResponse<int>.Success(count);
    }
}
