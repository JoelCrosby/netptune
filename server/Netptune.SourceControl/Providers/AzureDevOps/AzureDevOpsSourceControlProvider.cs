using Netptune.Core.SourceControl;

namespace Netptune.SourceControl.Providers.AzureDevOps;

internal sealed class AzureDevOpsSourceControlProvider : ISourceControlProvider
{
    private static readonly ScmTerminology AzureDevOpsTerminology = new()
    {
        PullRequest = "Pull request",
        PullRequests = "Pull requests",
        Issue = "Work item",
        CheckRuns = "Pipelines",
    };

    public SourceControlProvider Provider => SourceControlProvider.AzureDevOps;

    public ScmConnectMethod ConnectMethod => ScmConnectMethod.Token;

    public SourceControlCapabilities Capabilities => AzureDevOpsConnector.GrantedCapabilities;

    public ScmTerminology Terminology => AzureDevOpsTerminology;

    public ISourceControlConnector Connector { get; }

    public ISourceControlClient Client { get; }

    public ISourceControlWebhookReader? WebhookReader => null;

    public AzureDevOpsSourceControlProvider(AzureDevOpsConnector connector, AzureDevOpsSourceControlClient client)
    {
        Connector = connector;
        Client = client;
    }
}
