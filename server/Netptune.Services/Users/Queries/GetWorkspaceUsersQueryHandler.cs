using Mediator;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Queries;

public sealed record GetWorkspaceUsersQuery : IRequest<List<WorkspaceUserViewModel>>;

public sealed class GetWorkspaceUsersQueryHandler : IRequestHandler<GetWorkspaceUsersQuery, List<WorkspaceUserViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceUsersQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<WorkspaceUserViewModel>> Handle(GetWorkspaceUsersQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceAppUsers = await UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, true);

        if (workspaceAppUsers.Count == 0) return [];

        return workspaceAppUsers.ConvertAll(user => user.ToWorkspaceViewModel());
    }
}
