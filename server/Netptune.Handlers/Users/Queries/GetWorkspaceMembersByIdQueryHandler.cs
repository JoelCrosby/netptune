using Mediator;

using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Users.Queries;

public sealed record GetWorkspaceMembersByIdQuery(IReadOnlyCollection<string> UserIds) : IRequest<List<UserAvatar>>;

public sealed class GetWorkspaceMembersByIdQueryHandler : IRequestHandler<GetWorkspaceMembersByIdQuery, List<UserAvatar>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceMembersByIdQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<UserAvatar>> Handle(GetWorkspaceMembersByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.UserIds.Count == 0)
        {
            return [];
        }

        var workspaceId = await Identity.GetWorkspaceId();

        return await UnitOfWork.Users.GetUserAvatars(request.UserIds, workspaceId, cancellationToken);
    }
}
