using Mediator;
using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Events.Users;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Workspaces.Commands;

public sealed record LeaveWorkspaceCommand(string Key) : IRequest<ClientResponse>;

public sealed class LeaveWorkspaceCommandHandler : IRequestHandler<LeaveWorkspaceCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspaceUserCache WorkspaceUserCache;
    private readonly IActivityLogger Activity;

    public LeaveWorkspaceCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IWorkspaceUserCache workspaceUserCache,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        WorkspaceUserCache = workspaceUserCache;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(LeaveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var userEmail = Identity.GetCurrentUserEmail();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Key, true, cancellationToken);

        if (workspace is null) return ClientResponse.NotFound;

        if (userId == workspace.OwnerId)
        {
            return ClientResponse.Failed("the owner cannot leave the workspace");
        }

        WorkspaceUserCache.Remove(new() { UserId = userId, WorkspaceKey = request.Key });

        var removed = await UnitOfWork.Users.RemoveUsersFromWorkspace([userId], workspace.Id, cancellationToken);

        if (removed.Count == 0) return ClientResponse.Failed("you are not a member of this workspace");

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogWith<UserMembershipActivityMeta>(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Remove;
            options.Meta = new UserMembershipActivityMeta { Emails = [userEmail] };
        });

        return ClientResponse.Success;
    }
}
