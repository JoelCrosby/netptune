using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record SyncSourceControlReposCommand(int ConnectionId) : IRequest<ClientResponse<ScmRepoSyncResult>>;

public sealed class SyncSourceControlReposCommandHandler
    : IRequestHandler<SyncSourceControlReposCommand, ClientResponse<ScmRepoSyncResult>>
{
    private readonly IIdentityService Identity;
    private readonly ISourceControlRepoSync RepoSync;
    private readonly ILogger<SyncSourceControlReposCommandHandler> Logger;

    public SyncSourceControlReposCommandHandler(
        IIdentityService identity,
        ISourceControlRepoSync repoSync,
        ILogger<SyncSourceControlReposCommandHandler> logger)
    {
        Identity = identity;
        RepoSync = repoSync;
        Logger = logger;
    }

    public async ValueTask<ClientResponse<ScmRepoSyncResult>> Handle(
        SyncSourceControlReposCommand command,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        try
        {
            var result = await RepoSync.Sync(command.ConnectionId, workspaceId, cancellationToken);

            return result;
        }
        catch (ScmRateLimitedException exception)
        {
            Logger.LogWarning(exception, "Repository sync for {ConnectionId} was rate limited", command.ConnectionId);

            var message = $"{exception.Provider} is rate limiting Netptune. Try again after {exception.RetryAt:t}.";

            return ClientResponse<ScmRepoSyncResult>.Failed(message);
        }
        catch (ScmProviderException exception)
        {
            Logger.LogWarning(exception, "Repository sync for {ConnectionId} failed", command.ConnectionId);

            var message = $"{exception.Provider} could not list repositories. Try again shortly.";

            return ClientResponse<ScmRepoSyncResult>.Failed(message);
        }
    }
}
