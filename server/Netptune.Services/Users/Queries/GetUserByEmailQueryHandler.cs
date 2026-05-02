using Mediator;
using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Queries;

public sealed record GetUserByEmailQuery(string Email) : IRequest<UserViewModel?>;

public sealed class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, UserViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public GetUserByEmailQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
    }

    public async ValueTask<UserViewModel?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var user = await UnitOfWork.Users.GetByEmail(request.Email, true, cancellationToken);

        if (user is null) return null;

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceUser = await PermissionCache.GetUserPermissions(user.Id, workspaceKey);

        if (workspaceUser is null) return null;

        return user.ToViewModel(workspaceUser.Permissions);
    }
}
