namespace Netptune.Core.SourceControl;

public sealed class ScmConnectionContext
{
    private readonly ISourceControlConnector Connector;

    public ScmConnection Connection { get; }

    public SourceControlCapabilities Capabilities { get; }

    public ScmConnectionContext(
        ScmConnection connection,
        SourceControlCapabilities capabilities,
        ISourceControlConnector connector)
    {
        Connection = connection;
        Capabilities = capabilities;
        Connector = connector;
    }

    public ValueTask<ScmAccessToken> GetAccessToken(CancellationToken cancellationToken)
    {
        return Connector.GetAccessToken(Connection, cancellationToken);
    }
}
