using Mediator;
using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Events.Users;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Users.Commands;

public sealed record ToggleUserPermissionCommand(ToggleUserPermissionRequest Request) : IRequest<ClientResponse<List<string>>>;

public sealed class ToggleUserPermissionCommandHandler : IRequestHandler<ToggleUserPermissionCommand, ClientResponse<List<string>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IActivityLogger Activity;

    public ToggleUserPermissionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<List<string>>> Handle(ToggleUserPermissionCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true);

        if (workspace is null) return ClientResponse<List<string>>.Failed("workspace not found");

        var workspaceUser = await UnitOfWork.WorkspaceUsers.GetUserPermissions(request.Request.UserId, workspaceKey, false);

        if (workspaceUser is null) return ClientResponse<List<string>>.Failed("user is not a member of this workspace");

        var permissions = workspaceUser.Permissions;
        bool granted;

        if (permissions.Contains(request.Request.Permission))
        {
            permissions.Remove(request.Request.Permission);
            granted = false;
        }
        else
        {
            permissions.Add(request.Request.Permission);
            granted = true;
        }

        await UnitOfWork.WorkspaceUsers.SetUserPermissions(request.Request.UserId, workspace.Id, permissions);
        await UnitOfWork.CompleteAsync(cancellationToken);

        PermissionCache.Remove(new() { UserId = request.Request.UserId, WorkspaceKey = workspaceKey });

        Activity.LogWith<UserPermissionActivityMeta>(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.PermissionChanged;
            options.Meta = new UserPermissionActivityMeta
            {
                TargetUserId = request.Request.UserId,
                Permission = request.Request.Permission,
                Granted = granted,
            };
        });

        return ClientResponse<List<string>>.Success([.. permissions]);
    }
}
