using Flurl;
using Mediator;
using Netptune.Core.Extensions;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Users.Commands;

public sealed record ResendWorkspaceInviteCommand(string Email) : IRequest<ClientResponse>;

public sealed class ResendWorkspaceInviteCommandHandler : IRequestHandler<ResendWorkspaceInviteCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEmailService Email;
    private readonly IHostingService Hosting;

    public ResendWorkspaceInviteCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEmailService email,
        IHostingService hosting)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Email = email;
        Hosting = hosting;
    }

    public async ValueTask<ClientResponse> Handle(ResendWorkspaceInviteCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, false, cancellationToken);

        if (workspace is null) return ClientResponse.Failed("workspace not found");

        var email = request.Email.Trim().IdentityNormalize();
        var invite = await UnitOfWork.WorkspaceInvites.GetPendingByEmail(email, workspace.Id, cancellationToken);

        if (invite is null) return ClientResponse.Failed("no pending invite found for this email");

        invite.Code = Guid.NewGuid().ToString().Replace("-", "").ToLowerInvariant();
        invite.ExpiresAt = DateTime.UtcNow.AddDays(7);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var uri = Hosting.ClientOrigin
            .AppendPathSegments("auth", "register")
            .SetQueryParam("code", invite.Code, true)
            .SetQueryParam("refer", "invite");

        await Email.Send(new SendEmailModel
        {
            SendTo = new SendTo { Address = email, DisplayName = email },
            Name = "-name-",
            Action = "Go to Workspace",
            Link = uri,
            Reason = "workspace invite",
            Message = $"Hi you've been invited to join the {workspace.Name} workspace in Netptune.",
            Subject = "Netptune workspace invitation.",
            PreHeader = $"Hi you've been invited to join {workspace.Name} in Netptune.",
            RawTextContent = $"Hi you've been invited to join the {workspace.Name} workspace in Netptune. Click the link below to start. \n\n {uri}",
        });

        return ClientResponse.Success;
    }
}
