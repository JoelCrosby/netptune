using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Workspaces.Queries;

public sealed record GetUserWorkspacesQuery : IRequest<List<Workspace>>;

public sealed class GetUserWorkspacesQueryHandler : IRequestHandler<GetUserWorkspacesQuery, List<Workspace>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetUserWorkspacesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public ValueTask<List<Workspace>> Handle(GetUserWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        return new ValueTask<List<Workspace>>(UnitOfWork.Workspaces.GetUserWorkspaces(userId));
    }
}
