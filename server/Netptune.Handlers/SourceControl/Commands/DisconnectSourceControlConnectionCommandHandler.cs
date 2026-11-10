using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Events;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record DisconnectSourceControlConnectionCommand(int ConnectionId) : IRequest<ClientResponse>;

public sealed class DisconnectSourceControlConnectionCommandHandler
    : IRequestHandler<DisconnectSourceControlConnectionCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly ILogger<DisconnectSourceControlConnectionCommandHandler> Logger;

    public DisconnectSourceControlConnectionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventRecordWriter eventRecords,
        ISourceControlProviderRegistry registry,
        ILogger<DisconnectSourceControlConnectionCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventRecords = eventRecords;
        Registry = registry;
        Logger = logger;
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

        var scmConnection = connection.ToScmConnection();
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

        var disconnectedEvent = SourceControlConnectionEvents.Create(connection, EventKeys.SourceControlDisconnected);

        await UnitOfWork.Transaction(async () =>
        {
            await EventRecords.Append(disconnectedEvent, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        await DisconnectFromProvider(scmConnection, cancellationToken);

        return ClientResponse.Success;
    }

    // After the commit and best effort, so an unreachable provider can't keep a workspace connected and a
    // failed commit can't leave a connection whose webhooks are already gone. Webhooks left behind get
    // 410 Gone from the deleted connection, which tells the provider to stop sending them.
    private async Task DisconnectFromProvider(ScmConnection connection, CancellationToken cancellationToken)
    {
        var isEnabled = Registry.IsEnabled(connection.Provider);

        if (!isEnabled)
        {
            return;
        }

        try
        {
            var connector = Registry.Resolve(connection.Provider).Connector;

            await connector.Disconnect(connection, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(
                exception,
                "Disconnecting connection {ConnectionId} from {Provider} failed",
                connection.Id,
                connection.Provider);
        }
    }
}
