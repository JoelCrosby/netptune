using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record CompleteSourceControlConnectCommand(CompleteSourceControlConnectRequest Request)
    : IRequest<ClientResponse<SourceControlConnectionViewModel>>;

public sealed class CompleteSourceControlConnectCommandHandler
    : IRequestHandler<CompleteSourceControlConnectCommand, ClientResponse<SourceControlConnectionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly ISourceControlRepoSync RepoSync;
    private readonly ILogger<CompleteSourceControlConnectCommandHandler> Logger;

    public CompleteSourceControlConnectCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ISourceControlProviderRegistry registry,
        ISourceControlRepoSync repoSync,
        ILogger<CompleteSourceControlConnectCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Registry = registry;
        RepoSync = repoSync;
        Logger = logger;
    }

    public async ValueTask<ClientResponse<SourceControlConnectionViewModel>> Handle(
        CompleteSourceControlConnectCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var isEnabled = Registry.IsEnabled(request.Provider);

        if (!isEnabled)
        {
            var message = $"{request.Provider} is not enabled on this Netptune instance.";

            return ClientResponse<SourceControlConnectionViewModel>.Failed(message);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();

        var callback = new ScmConnectCallback
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Parameters = request.Parameters,
        };

        var connected = await CompleteWithProvider(request.Provider, callback, cancellationToken);

        if (!connected.IsSuccess)
        {
            return ClientResponse<SourceControlConnectionViewModel>.Failed(connected.Message);
        }

        var connection = await SaveConnection(
            request.Provider,
            workspaceId,
            userId,
            connected.Payload!,
            cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);
        await SyncRepos(connection, cancellationToken);

        var linkedRepos = await UnitOfWork.SourceControlRepos.GetLinkedForWorkspace(workspaceId, cancellationToken);
        var linkedCount = linkedRepos.Count(repo => repo.ConnectionId == connection.Id);
        var viewModel = SourceControlConnectionMapper.ToViewModel(connection, Registry, linkedCount);

        return ClientResponse<SourceControlConnectionViewModel>.Success(viewModel);
    }

    private async Task<ClientResponse<ScmConnectResult>> CompleteWithProvider(
        SourceControlProvider providerKind,
        ScmConnectCallback callback,
        CancellationToken cancellationToken)
    {
        var provider = Registry.Resolve(providerKind);

        try
        {
            var result = await provider.Connector.CompleteConnect(callback, cancellationToken);

            return result;
        }
        catch (Exception exception) when (exception is ScmProviderException or ScmRateLimitedException)
        {
            Logger.LogWarning(exception, "{Provider} rejected a connection attempt", providerKind);

            var message = $"{providerKind} could not complete the connection. Try again shortly.";

            return ClientResponse<ScmConnectResult>.Failed(message);
        }
    }

    private async Task<SourceControlConnection> SaveConnection(
        SourceControlProvider provider,
        int workspaceId,
        string userId,
        ScmConnectResult result,
        CancellationToken cancellationToken)
    {
        var key = new SourceControlAccountKey
        {
            WorkspaceId = workspaceId,
            Provider = provider,
            ExternalAccountId = result.ExternalAccountId,
        };

        var existing = await UnitOfWork.SourceControlConnections.GetByAccount(key, cancellationToken);

        var connection = existing ?? await UnitOfWork.SourceControlConnections.AddAsync(new SourceControlConnection
        {
            WorkspaceId = workspaceId,
            Provider = provider,
            ExternalAccountId = result.ExternalAccountId,
            AccountLogin = result.Account.Login,
            OwnerId = userId,
            CreatedByUserId = userId,
        }, cancellationToken);

        connection.AccountLogin = result.Account.Login;
        connection.AccountType = result.Account.AccountType;
        connection.AccountAvatarUrl = result.Account.AvatarUrl;
        connection.Status = ScmConnectionStatus.Active;
        connection.GrantedCapabilities = result.GrantedCapabilities;
        connection.CredentialKind = result.CredentialKind;
        connection.Credential = result.ProtectedCredential;
        connection.ModifiedByUserId = userId;

        return connection;
    }

    // The connection is already saved, so a failed listing shouldn't undo it; the settings page can
    // retry the sync on its own.
    private async Task SyncRepos(SourceControlConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RepoSync.Sync(connection.Id, connection.WorkspaceId, cancellationToken);

            if (!result.IsSuccess)
            {
                Logger.LogWarning(
                    "Repository sync after connecting {ConnectionId} failed: {Message}",
                    connection.Id,
                    result.Message);
            }
        }
        catch (Exception exception) when (exception is ScmProviderException or ScmRateLimitedException)
        {
            Logger.LogWarning(exception, "Repository sync after connecting {ConnectionId} failed", connection.Id);
        }
    }
}
