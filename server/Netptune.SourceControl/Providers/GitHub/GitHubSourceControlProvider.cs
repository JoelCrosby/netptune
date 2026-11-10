using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Checks;

namespace Netptune.SourceControl.Providers.GitHub;

internal sealed class GitHubSourceControlProvider : ISourceControlProvider
{
    private static readonly ScmTerminology GitHubTerminology = new()
    {
        PullRequest = "Pull request",
        PullRequests = "Pull requests",
        Issue = "Issue",
        CheckRuns = "Checks",
    };

    public SourceControlProvider Provider => SourceControlProvider.GitHub;

    public ScmConnectMethod ConnectMethod => ScmConnectMethod.Redirect;

    public SourceControlCapabilities Capabilities =>
        SourceControlCapabilities.ListRepositories
        | SourceControlCapabilities.ReadBranches
        | SourceControlCapabilities.ReadCommits
        | SourceControlCapabilities.ReadPullRequests
        | SourceControlCapabilities.ReadCheckRuns
        | SourceControlCapabilities.Webhooks
        | SourceControlCapabilities.SearchCode;

    public ScmTerminology Terminology => GitHubTerminology;

    public ISourceControlConnector Connector { get; }

    public ISourceControlClient Client { get; }

    public ISourceControlWebhookReader? WebhookReader { get; }

    public IScmConnectionChecker? ConnectionChecker => null;

    public GitHubSourceControlProvider(
        GitHubConnector connector,
        GitHubSourceControlClient client,
        GitHubWebhookReader webhookReader)
    {
        Connector = connector;
        Client = client;
        WebhookReader = webhookReader;
    }
}
