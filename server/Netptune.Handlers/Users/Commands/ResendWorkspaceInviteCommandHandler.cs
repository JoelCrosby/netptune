using System.Globalization;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Extensions;
using Netptune.Core.Localization;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Localization;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Users.Commands;

public sealed record ResendWorkspaceInviteCommand(string Email) : IRequest<ClientResponse>;

public sealed class ResendWorkspaceInviteCommandHandler : IRequestHandler<ResendWorkspaceInviteCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEmailService Email;
    private readonly IHostingService Hosting;
    private readonly IUserLocaleReader LocaleReader;

    public ResendWorkspaceInviteCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEmailService email,
        IHostingService hosting,
        IUserLocaleReader localeReader)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Email = email;
        Hosting = hosting;
        LocaleReader = localeReader;
    }

    public async ValueTask<ClientResponse> Handle(ResendWorkspaceInviteCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, false, cancellationToken);

        if (workspace is null) return ClientResponse.Failed(AppText.ErrorWorkspaceNotFound);

        var normalizedEmail = request.Email.Trim().IdentityNormalize();
        var invite = await UnitOfWork.WorkspaceInvites.GetPendingByEmail(normalizedEmail, workspace.Id, cancellationToken);

        if (invite is null) return ClientResponse.Failed(AppText.ErrorNoPendingInviteFoundEmail);

        invite.Code = Guid.NewGuid().ToString().Replace("-", "").ToLowerInvariant();
        invite.ExpiresAt = DateTime.UtcNow.AddDays(7);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var recipient = await UnitOfWork.Users.GetByEmail(invite.Email, true, cancellationToken);
        var hasAccount = recipient?.UserType == AppUserType.User;
        var link = WorkspaceInviteLinks.Create(Hosting.ClientOrigin, invite.Code, hasAccount);
        var locale = await ReadRecipientLocale(hasAccount ? recipient : null, cancellationToken);

        using (CultureScope.Use(locale))
        {
            var message = EmailMessages.WorkspaceInvite(invite.Email, workspace.Name, link);

            await Email.Send(message);
        }

        return ClientResponse.Success;
    }

    private async Task<string> ReadRecipientLocale(AppUser? recipient, CancellationToken cancellationToken)
    {
        if (recipient is null)
        {
            return CultureInfo.CurrentUICulture.Name;
        }

        return await LocaleReader.GetLocale(recipient.Id, cancellationToken);
    }
}
