using System.Globalization;

using Flurl;

using Mediator;

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

        var email = request.Email.Trim().IdentityNormalize();
        var invite = await UnitOfWork.WorkspaceInvites.GetPendingByEmail(email, workspace.Id, cancellationToken);

        if (invite is null) return ClientResponse.Failed(AppText.ErrorNoPendingInviteFoundEmail);

        invite.Code = Guid.NewGuid().ToString().Replace("-", "").ToLowerInvariant();
        invite.ExpiresAt = DateTime.UtcNow.AddDays(7);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var link = Hosting.ClientOrigin
            .AppendPathSegments("auth", "register")
            .SetQueryParam("code", invite.Code, true)
            .SetQueryParam("refer", "invite");
        var locale = await ReadRecipientLocale(email, cancellationToken);

        using (CultureScope.Use(locale))
        {
            var message = EmailMessages.WorkspaceInvite(email, workspace.Name, link);

            await Email.Send(message);
        }

        return ClientResponse.Success;
    }

    private async Task<string> ReadRecipientLocale(string email, CancellationToken cancellationToken)
    {
        var user = await UnitOfWork.Users.GetByEmail(email, true, cancellationToken);

        if (user is null)
        {
            return CultureInfo.CurrentUICulture.Name;
        }

        return await LocaleReader.GetLocale(user.Id, cancellationToken);
    }
}
