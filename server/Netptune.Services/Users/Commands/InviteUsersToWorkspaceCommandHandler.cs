using Flurl;
using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Users;
using Netptune.Core.Extensions;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Relationships;
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
    private readonly IActivityLogger Activity;

    public InviteUsersToWorkspaceCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEmailService email,
        IHostingService hosting,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Email = email;
        Hosting = hosting;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<InviteUserResponse>> Handle(InviteUsersToWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var emailList = request.Emails.Select(e => e.Trim().IdentityNormalize()).ToHashSet();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true, cancellationToken);

        if (workspace is null) return ClientResponse<InviteUserResponse>.Failed("workspace not found");
        if (emailList.Count == 0) return ClientResponse<InviteUserResponse>.Failed("no email addresses provided");

        var users = await UnitOfWork.Users.GetByEmailRange(emailList, true, cancellationToken);
        var userIds = users.ConvertAll(user => user.Id);
        var existingUsers = await UnitOfWork.Users.IsUserInWorkspaceRange(userIds, workspace.Id, cancellationToken);
        var newUserIds = userIds.Except(existingUsers.Select(x => x.Id)).ToHashSet();

        await UnitOfWork.Users.InviteUsersToWorkspace(newUserIds, workspace.Id, cancellationToken);

        var existingUserEmails = existingUsers.Select(user => user.Email!.IdentityNormalize()).ToHashSet();
        var usersToInvite = emailList.Where(email => !existingUserEmails.Contains(email)).Select(e => e.ToLowerInvariant()).ToHashSet();

        var currentUserId = Identity.GetCurrentUserId();

        var invites = await CreateOrRefreshInvites(usersToInvite, workspace.Id, currentUserId, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        await SendUserInviteEmails(invites, workspace);

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

    private async Task<List<WorkspaceInvite>> CreateOrRefreshInvites(IEnumerable<string> emails, int workspaceId, string? invitedByUserId, CancellationToken cancellationToken)
    {
        var emailList = emails.ToList();
        var existing = await UnitOfWork.WorkspaceInvites.GetPendingByEmailRange(emailList, workspaceId, cancellationToken);
        var existingByEmail = existing.ToDictionary(x => x.Email);

        var now = DateTime.UtcNow;
        var expiry = now.AddDays(7);

        foreach (var invite in existing)
        {
            invite.Code = GenerateCode();
            invite.ExpiresAt = expiry;
        }

        var newInvites = emailList
            .Where(email => !existingByEmail.ContainsKey(email))
            .Select(email => new WorkspaceInvite
            {
                Email = email,
                WorkspaceId = workspaceId,
                Code = GenerateCode(),
                InvitedByUserId = invitedByUserId,
                CreatedAt = now,
                ExpiresAt = expiry,
            })
            .ToList();

        await UnitOfWork.WorkspaceInvites.AddRangeAsync(newInvites, cancellationToken);

        return [..existing, ..newInvites];
    }

    private Task SendUserInviteEmails(List<WorkspaceInvite> invites, Workspace workspace)
    {
        if (invites.Count == 0) return Task.CompletedTask;

        var tasks = invites.Select(invite =>
        {
            var uri = Hosting.ClientOrigin
                .AppendPathSegments("auth", "register")
                .SetQueryParam("code", invite.Code, true)
                .SetQueryParam("refer", "invite");

            return Email.Send(new SendEmailModel
            {
                SendTo = new SendTo { Address = invite.Email, DisplayName = invite.Email },
                Name = "-name-",
                Action = "Go to Workspace",
                Link = uri,
                Reason = "workspace invite",
                Message = $"Hi you've been invited to join the {workspace.Name} workspace in Netptune.",
                Subject = "Netptune workspace invitation.",
                PreHeader = $"Hi you've been invited to join {workspace.Name} in Netptune.",
                RawTextContent = $"Hi you've been invited to join the {workspace.Name} workspace in Netptune. Click the link below to start. \n\n {uri}",
            });
        });

        return Task.WhenAll(tasks);
    }

    private static string GenerateCode() =>
        Guid.NewGuid().ToString().Replace("-", "").ToLowerInvariant();
}
