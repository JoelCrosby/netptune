using Flurl;
using Mediator;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Users;
using Netptune.Core.Extensions;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Users.Commands;

public sealed record InviteUsersToWorkspaceCommand(IEnumerable<string> Emails) : IRequest<ClientResponse<InviteUserResponse>>;

public sealed class InviteUsersToWorkspaceCommandHandler : IRequestHandler<InviteUsersToWorkspaceCommand, ClientResponse<InviteUserResponse>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEmailService Email;
    private readonly IHostingService Hosting;
    private readonly IInviteCache InviteCache;
    private readonly IActivityLogger Activity;

    public InviteUsersToWorkspaceCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEmailService email,
        IHostingService hosting,
        IInviteCache inviteCache,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Email = email;
        Hosting = hosting;
        InviteCache = inviteCache;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<InviteUserResponse>> Handle(InviteUsersToWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var emailList = request.Emails.Select(e => e.Trim().IdentityNormalize()).ToHashSet();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true);

        if (workspace is null) return ClientResponse<InviteUserResponse>.Failed("workspace not found");
        if (emailList.Count == 0) return ClientResponse<InviteUserResponse>.Failed("no email addresses provided");

        var users = await UnitOfWork.Users.GetByEmailRange(emailList, true);
        var userIds = users.ConvertAll(user => user.Id);
        var existingUsers = await UnitOfWork.Users.IsUserInWorkspaceRange(userIds, workspace.Id);
        var newUserIds = userIds.Except(existingUsers.Select(x => x.Id)).ToHashSet();

        await UnitOfWork.Users.InviteUsersToWorkspace(newUserIds, workspace.Id);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var existingUserEmails = existingUsers.Select(user => user.Email!.IdentityNormalize()).ToHashSet();
        var usersToInvite = emailList.Where(email => !existingUserEmails.Contains(email)).Select(e => e.ToLowerInvariant()).ToHashSet();

        await SendUserInviteEmails(usersToInvite, workspace);

        Activity.LogWith<UserMembershipActivityMeta>(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Invite;
            options.Meta = new UserMembershipActivityMeta { Emails = usersToInvite.ToList() };
        });

        return new InviteUserResponse { Emails = usersToInvite.ToList() };
    }

    private Task SendUserInviteEmails(IEnumerable<string> emails, Workspace workspace)
    {
        var emailList = emails.ToList();

        var key = Guid.NewGuid().ToString().Replace("-", "").ToLowerInvariant();

        foreach (var email in emailList)
        {
            InviteCache.Create(key, new() { Email = email, WorkspaceId = workspace.Id });
        }

        var uri = Hosting.ClientOrigin
            .AppendPathSegments("auth", "register")
            .SetQueryParam("code", key, true)
            .SetQueryParam("refer", "invite");

        var sendTo = emailList.Select(e => new SendTo { Address = e, DisplayName = e }).ToList();

        return Email.Send(new SendMultipleEmailModel
        {
            SendTo = sendTo,
            Name = "-name-",
            Action = "Go to Workspace",
            Link = uri,
            Reason = "workspace invite",
            Message = "Hi you've been invited to join the -workspace- workspace in Netptune.",
            Subject = "Netptune workspace invitation.",
            PreHeader = "Hi you've been invited to join the -workspace- in Netptune.",
            RawTextContent = $"Hi you've been invited to join the -workspace- in Netptune. Click the link below to start. \n\n {uri}",
        });
    }
}
