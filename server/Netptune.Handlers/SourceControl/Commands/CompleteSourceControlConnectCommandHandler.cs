using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Localization;
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
    : IRequest<ClientResponse<SourceControlConnectCompletionViewModel>>;

public sealed class CompleteSourceControlConnectCommandHandler
    : IRequestHandler<CompleteSourceControlConnectCommand, ClientResponse<SourceControlConnectCompletionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly ISourceControlRepoSync RepoSync;
    private readonly IEventRecordWriter EventRecords;
    private readonly ILogger<CompleteSourceControlConnectCommandHandler> Logger;

    public CompleteSourceControlConnectCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ISourceControlProviderRegistry registry,
        ISourceControlRepoSync repoSync,
        IEventRecordWriter eventRecords,
        ILogger<CompleteSourceControlConnectCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Registry = registry;
        RepoSync = repoSync;
        EventRecords = eventRecords;
        Logger = logger;
    }

    public async ValueTask<ClientResponse<SourceControlConnectCompletionViewModel>> Handle(
        CompleteSourceControlConnectCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var isEnabled = Registry.IsEnabled(request.Provider);

        if (!isEnabled)
        {
            var message = Localized.Format(AppText.ErrorNotEnabledNetptuneInstance, request.Provider);

            return ClientResponse<SourceControlConnectCompletionViewModel>.Failed(message);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();

        var callback = new ScmConnectCallback
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Parameters = request.Parameters,
        };

        var completed = await CompleteWithProvider(request.Provider, callback, cancellationToken);

        if (!completed.IsSuccess)
        {
            return ClientResponse<SourceControlConnectCompletionViewModel>.Failed(completed.Message);
        }

        var outcome = completed.Payload!;

        if (outcome.Choice is not null)
        {
            var choice = await ToChoiceViewModel(request.Provider, workspaceId, outcome.Choice, cancellationToken);
            var choiceCompletion = new SourceControlConnectCompletionViewModel { Choice = choice };

            return ClientResponse<SourceControlConnectCompletionViewModel>.Success(choiceCompletion);
        }

        var connection = await UnitOfWork.Transaction(async () =>
        {
            var saved = await SaveConnection(
                request.Provider,
                workspaceId,
                userId,
                outcome.Connection!,
                cancellationToken);

            // Saved first so a new connection has the id the audit event refers to.
            await UnitOfWork.CompleteAsync(cancellationToken);

            var connectedEvent = SourceControlConnectionEvents.Create(saved, EventKeys.SourceControlConnected);

            await EventRecords.Append(connectedEvent, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            return saved;
        });

        await SyncRepos(connection, cancellationToken);

        var linkedRepos = await UnitOfWork.SourceControlRepos.GetLinkedForWorkspace(workspaceId, cancellationToken);
        var linkedCount = linkedRepos.Count(repo => repo.ConnectionId == connection.Id);
        var viewModel = SourceControlConnectionMapper.ToViewModel(connection, Registry, linkedCount);
        var completion = new SourceControlConnectCompletionViewModel { Connection = viewModel };

        return ClientResponse<SourceControlConnectCompletionViewModel>.Success(completion);
    }

    private async Task<SourceControlConnectChoiceViewModel> ToChoiceViewModel(
        SourceControlProvider provider,
        int workspaceId,
        ScmConnectChoice choice,
        CancellationToken cancellationToken)
    {
        var workspaceConnections = await UnitOfWork.SourceControlConnections.GetForWorkspace(
            workspaceId,
            cancellationToken);

        var connectedAccountIds = workspaceConnections
            .Where(connection => connection.Provider == provider)
            .Select(connection => connection.ExternalAccountId)
            .ToHashSet();

        var candidates = choice.Candidates
            .Where(candidate => !connectedAccountIds.Contains(candidate.ExternalAccountId))
            .Select(candidate => new SourceControlConnectCandidateViewModel
            {
                AccountLogin = candidate.Account.Login,
                AccountType = candidate.Account.AccountType,
                AccountAvatarUrl = candidate.Account.AvatarUrl,
                CallbackParameters = new Dictionary<string, string>(candidate.CallbackParameters),
            })
            .ToList();

        return new SourceControlConnectChoiceViewModel
        {
            Candidates = candidates,
            InstallUrl = choice.InstallUrl,
        };
    }

    private async Task<ClientResponse<ScmConnectOutcome>> CompleteWithProvider(
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

            var message = Localized.Format(AppText.ErrorCouldNotCompleteConnectionTryAgainShortly, providerKind);

            return ClientResponse<ScmConnectOutcome>.Failed(message);
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

        var connection = existing ?? new SourceControlConnection
        {
            WorkspaceId = workspaceId,
            Provider = provider,
            ExternalAccountId = result.ExternalAccountId,
            AccountLogin = result.Account.Login,
            OwnerId = userId,
            CreatedByUserId = userId,
        };

        if (existing is null)
        {
            await UnitOfWork.SourceControlConnections.AddAsync(connection, cancellationToken);
        }

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
