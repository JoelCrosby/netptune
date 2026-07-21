using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Commands;

public sealed record UpdateWorkspaceRoleCommand(UpdateWorkspaceRoleRequest Request)
    : IRequest<ClientResponse<WorkspaceRoleUpdateViewModel>>;

public sealed class UpdateWorkspaceRoleCommandHandler
    : IRequestHandler<UpdateWorkspaceRoleCommand, ClientResponse<WorkspaceRoleUpdateViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IEventRecordWriter EventRecords;

    public UpdateWorkspaceRoleCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<WorkspaceRoleUpdateViewModel>> Handle(
        UpdateWorkspaceRoleCommand request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (!Enum.IsDefined(req.Role) || req.Role == WorkspaceRole.Owner)
        {
            return ClientResponse<WorkspaceRoleUpdateViewModel>.Failed("owner assignment requires an ownership transfer");
        }

        if (req.UserId == Identity.GetCurrentUserId())
        {
            return ClientResponse<WorkspaceRoleUpdateViewModel>.Failed("you cannot change your own workspace role");
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true, cancellationToken);

        if (workspace is null)
        {
            return ClientResponse<WorkspaceRoleUpdateViewModel>.Failed("workspace not found");
        }

        var workspaceUser = await UnitOfWork.WorkspaceUsers
            .GetUserPermissions(req.UserId, workspaceKey, false, cancellationToken);

        if (workspaceUser is null)
        {
            return ClientResponse<WorkspaceRoleUpdateViewModel>.Failed("user is not a member of this workspace");
        }

        if (workspaceUser.Role == WorkspaceRole.Owner)
        {
            return ClientResponse<WorkspaceRoleUpdateViewModel>.Failed("the workspace owner role cannot be changed here");
        }

        var oldRole = workspaceUser.Role;
        var permissions = WorkspaceRolePermissions.GetDefaultPermissions(req.Role).ToList();

        await UnitOfWork.WorkspaceUsers.SetUserRole(
            req.UserId,
            workspace.Id,
            req.Role,
            permissions,
            cancellationToken);

        await EventRecords.Append(new EventWriteRequest<WorkspaceRoleChangedPayload>
        {
            WorkspaceId = workspace.Id,
            EventKey = EventKeys.WorkspaceRoleChanged,
            SubjectType = EventEntityTypes.From(EntityType.Workspace),
            SubjectId = workspace.Id.ToString(),
            Payload = new WorkspaceRoleChangedPayload
            {
                TargetUserId = req.UserId,
                OldRole = oldRole.ToString(),
                NewRole = req.Role.ToString(),
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Member,
                    EntityType = EventEntityTypes.From(EntityType.User),
                    EntityId = req.UserId,
                },
            ],
        }, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        PermissionCache.Remove(new WorkspaceUserKey
        {
            UserId = req.UserId,
            WorkspaceKey = workspaceKey,
        });

        return ClientResponse<WorkspaceRoleUpdateViewModel>.Success(new WorkspaceRoleUpdateViewModel
        {
            Role = req.Role,
            Permissions = permissions,
        });
    }
}
