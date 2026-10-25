using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl.Connect;

namespace Netptune.Core.SourceControl;

public interface ISourceControlConnector
{
    ScmConnectStart BeginConnect(ScmConnectRequest request);

    ValueTask<ClientResponse<ScmConnectOutcome>> CompleteConnect(ScmConnectCallback callback, CancellationToken cancellationToken);

    ValueTask<ScmAccessToken> GetAccessToken(ScmConnection connection, CancellationToken cancellationToken);

    ValueTask<SourceControlCapabilities> GetGrantedCapabilities(ScmConnection connection, CancellationToken cancellationToken);
}
