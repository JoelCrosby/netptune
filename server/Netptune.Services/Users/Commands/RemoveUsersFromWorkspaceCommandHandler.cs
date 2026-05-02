using Mediator;
using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Events.Users;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Users.Commands;

public sealed record RemoveUsersFromWorkspaceCommand(IEnumerable<string> Emails) : IRequest<ClientResponse<RemoveUsersWorkspaceResponse>>;

public sealed class RemoveUsersFromWorkspaceCommandHandler : IRequestHandler<RemoveUsersFromWorkspaceCommand, ClientResponse<RemoveUsersWorkspaceResponse>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspaceUserCache WorkspaceUserCache;
    private readonly IActivityLogger Activity;

    public RemoveUsersFromWorkspaceCommandHandler(
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

    public async ValueTask<ClientResponse<RemoveUsersWorkspaceResponse>> Handle(RemoveUsersFromWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var emailList = request.Emails.ToList();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true, cancellationToken);

        if (workspace is null) return ClientResponse<RemoveUsersWorkspaceResponse>.Failed("workspace not found");
        if (emailList.Count == 0) return ClientResponse<RemoveUsersWorkspaceResponse>.Failed("no email addresses provided");

        var users = await UnitOfWork.Users.GetByEmailRange(emailList, true, cancellationToken);
        var userIds = users.ConvertAll(user => user.Id);

        if (userIds.Count == 1 && userIds.Contains(workspace.OwnerId!))
        {
            return ClientResponse<RemoveUsersWorkspaceResponse>.Failed("cannot remove thew owner of the workspace");
        }

        foreach (var userId in userIds)
        {
            if (userId == workspace.OwnerId) continue;

            WorkspaceUserCache.Remove(new() { UserId = userId, WorkspaceKey = workspaceKey });
        }

        var removed = await UnitOfWork.Users.RemoveUsersFromWorkspace(userIds, workspace.Id, cancellationToken);
        var removeUserEmails = removed.Select(x => x.User.Email!).ToList();

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogWith<UserMembershipActivityMeta>(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Remove;
            options.Meta = new UserMembershipActivityMeta { Emails = removeUserEmails };
        });

        return new RemoveUsersWorkspaceResponse { Emails = removeUserEmails };
    }
}
