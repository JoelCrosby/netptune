using System.Globalization;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Users;
using Netptune.Core.Extensions;
using Netptune.Core.Localization;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Relationships;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Localization;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Users.Commands;

public sealed record InviteUsersToWorkspaceCommand(IEnumerable<string> Emails) : IRequest<ClientResponse<InviteUserResponse>>;

public sealed class InviteUsersToWorkspaceCommandHandler : IRequestHandler<InviteUsersToWorkspaceCommand, ClientResponse<InviteUserResponse>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEmailService Email;
    private readonly IHostingService Hosting;
    private readonly IActivityLogger Activity;
    private readonly IUserLocaleReader LocaleReader;

    public InviteUsersToWorkspaceCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEmailService email,
        IHostingService hosting,
        IActivityLogger activity,
        IUserLocaleReader localeReader)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Email = email;
        Hosting = hosting;
        Activity = activity;
        LocaleReader = localeReader;
    }

    public async ValueTask<ClientResponse<InviteUserResponse>> Handle(InviteUsersToWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var emailList = request.Emails
            .Select(email => email.Trim().Normalize().ToLowerInvariant())
            .Where(email => email.Length > 0)
            .ToHashSet();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true, cancellationToken);

        if (workspace is null) return ClientResponse<InviteUserResponse>.Failed(AppText.ErrorWorkspaceNotFound);

        if (emailList.Count == 0)
        {
            return ClientResponse<InviteUserResponse>.Failed(AppText.ErrorNoEmailAddressesProvided);
        }

        var matchingUsers = await UnitOfWork.Users.GetByEmailRange(emailList, true, cancellationToken);
        var accounts = matchingUsers.Where(user => user.UserType == AppUserType.User).ToList();
        var serviceAccountEmails = matchingUsers
            .Where(user => user.UserType != AppUserType.User)
            .Select(user => user.Email!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var accountIds = accounts.ConvertAll(user => user.Id);
        var members = await UnitOfWork.Users.IsUserInWorkspaceRange(accountIds, workspace.Id, cancellationToken);
        var memberEmails = members
            .Select(user => user.Email!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Existing accounts are invited rather than added outright: an account only proves its owner
        // controls the address once they redeem the code that was emailed to it.
        var emailsToInvite = emailList
            .Where(email => !memberEmails.Contains(email) && !serviceAccountEmails.Contains(email))
            .ToHashSet();

        var currentUserId = Identity.GetCurrentUserId();

        var invites = await CreateOrRefreshInvites(emailsToInvite, workspace.Id, currentUserId, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var localesByEmail = await ReadLocalesByEmail(accounts, cancellationToken);

        await SendUserInviteEmails(invites, workspace, localesByEmail);

        Activity.LogWith<UserMembershipActivityMeta>(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Invite;
            options.Meta = new UserMembershipActivityMeta { Emails = emailsToInvite.ToList() };
        });

        return new InviteUserResponse { Emails = emailsToInvite.ToList() };
    }

    private async Task<List<WorkspaceInvite>> CreateOrRefreshInvites(IEnumerable<string> emails, int workspaceId, string? invitedByUserId, CancellationToken cancellationToken)
    {
        var emailList = emails.ToList();
        var existing = await UnitOfWork.WorkspaceInvites.GetPendingByEmailRange(emailList, workspaceId, cancellationToken);
        var existingByEmail = existing
            .GroupBy(invite => invite.Email, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

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

        return [.. existingByEmail.Values, .. newInvites];
    }

    private async Task<Dictionary<string, string>> ReadLocalesByEmail(
        List<AppUser> users,
        CancellationToken cancellationToken)
    {
        var userIds = users.ConvertAll(user => user.Id);
        var locales = await LocaleReader.GetLocales(userIds, cancellationToken);

        return users
            .Where(user => user.Email is not null)
            .ToDictionary(user => user.Email!, user => locales[user.Id], StringComparer.OrdinalIgnoreCase);
    }

    private Task SendUserInviteEmails(
        List<WorkspaceInvite> invites,
        Workspace workspace,
        Dictionary<string, string> localesByEmail)
    {
        if (invites.Count == 0) return Task.CompletedTask;

        var inviterLocale = CultureInfo.CurrentUICulture.Name;
        var tasks = invites.Select(invite =>
        {
            var hasAccount = localesByEmail.TryGetValue(invite.Email, out var recipientLocale);
            var link = WorkspaceInviteLinks.Create(Hosting.ClientOrigin, invite.Code, hasAccount);

            using var culture = CultureScope.Use(recipientLocale ?? inviterLocale);

            var email = EmailMessages.WorkspaceInvite(invite.Email, workspace.Name, link);

            return Email.Send(email);
        });

        return Task.WhenAll(tasks);
    }

    private static string GenerateCode() =>
        Guid.NewGuid().ToString().Replace("-", "").ToLowerInvariant();
}
