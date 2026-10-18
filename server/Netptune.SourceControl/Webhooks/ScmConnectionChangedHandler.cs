using Netptune.Core.Entities;
using Netptune.Core.Services.Realtime;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Events;

namespace Netptune.SourceControl.Webhooks;

internal sealed class ScmConnectionChangedHandler : IScmEventHandler
{
    private readonly ISourceControlProviderRegistry Registry;
    private readonly ISourceControlRepoSync RepoSync;

    public ScmConnectionChangedHandler(ISourceControlProviderRegistry registry, ISourceControlRepoSync repoSync)
    {
        Registry = registry;
        RepoSync = repoSync;
    }

    public string RefreshScope => WorkspaceEventScopes.SourceControl;

    public bool CanHandle(ScmEvent scmEvent)
    {
        return scmEvent is ScmConnectionChanged;
    }

    public async Task<bool> Handle(
        ScmEvent scmEvent,
        SourceControlConnection connection,
        ScmEventSource source,
        CancellationToken cancellationToken)
    {
        var changed = (ScmConnectionChanged)scmEvent;

        switch (changed.Action)
        {
            case ScmConnectionAction.Revoked:
                connection.Status = ScmConnectionStatus.Revoked;

                return true;

            case ScmConnectionAction.Suspended:
                connection.Status = ScmConnectionStatus.Suspended;

                return true;

            case ScmConnectionAction.Unsuspended:
                connection.Status = ScmConnectionStatus.Active;

                return true;

            case ScmConnectionAction.PermissionsChanged:
            {
                var provider = Registry.Resolve(connection.Provider);
                var scmConnection = connection.ToScmConnection();
                var granted = await provider.Connector.GetGrantedCapabilities(scmConnection, cancellationToken);

                connection.GrantedCapabilities = granted;

                return true;
            }

            case ScmConnectionAction.RepositoriesAdded:
            case ScmConnectionAction.RepositoriesRemoved:
            {
                var synced = await RepoSync.Sync(connection.Id, connection.WorkspaceId, cancellationToken);

                return synced.IsSuccess;
            }

            default:
                return false;
        }
    }
}
