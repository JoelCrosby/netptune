using Mediator;
using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Queries;

public sealed record GetUserQuery(string UserId) : IRequest<UserViewModel?>;

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public GetUserQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
    }

    public async ValueTask<UserViewModel?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await UnitOfWork.Users.GetAsync(request.UserId, true);

        if (user is null) return null;

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceUser = await PermissionCache.GetUserPermissions(user.Id, workspaceKey);

        if (workspaceUser is null) return null;

        return user.ToViewModel(workspaceUser.Permissions);
    }
}
