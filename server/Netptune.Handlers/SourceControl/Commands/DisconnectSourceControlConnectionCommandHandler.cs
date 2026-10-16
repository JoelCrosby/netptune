using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record DisconnectSourceControlConnectionCommand(int ConnectionId) : IRequest<ClientResponse>;

public sealed class DisconnectSourceControlConnectionCommandHandler
    : IRequestHandler<DisconnectSourceControlConnectionCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DisconnectSourceControlConnectionCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        DisconnectSourceControlConnectionCommand command,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var connection = await UnitOfWork.SourceControlConnections.GetOwned(
            command.ConnectionId,
            workspaceId,
            cancellationToken);

        if (connection is null)
        {
            return ClientResponse.NotFound;
        }

        var repos = await UnitOfWork.SourceControlRepos.GetForConnection(connection.Id, cancellationToken);

        foreach (var repo in repos)
        {
            repo.IsLinked = false;
            repo.LinkedAt = null;
            repo.LinkedByUserId = null;
        }

        // The row is kept for history, but nothing that could authenticate as the workspace is.
        connection.Credential = null;
        connection.CredentialHint = null;
        connection.WebhookSecret = null;
        connection.Delete(userId);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
