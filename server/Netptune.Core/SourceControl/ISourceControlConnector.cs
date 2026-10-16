using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl.Connect;

namespace Netptune.Core.SourceControl;

public interface ISourceControlConnector
{
    ScmConnectStart BeginConnect(ScmConnectRequest request);

    ValueTask<ClientResponse<ScmConnectResult>> CompleteConnect(ScmConnectCallback callback, CancellationToken cancellationToken);

    ValueTask<ScmAccessToken> GetAccessToken(ScmConnection connection, CancellationToken cancellationToken);
}
