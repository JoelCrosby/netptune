using Netptune.Core.Responses.Common;

namespace Netptune.Core.SourceControl;

public interface ISourceControlRepoSync
{
    ValueTask<ClientResponse<ScmRepoSyncResult>> Sync(int connectionId, int workspaceId, CancellationToken cancellationToken);
}
