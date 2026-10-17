using System.Runtime.CompilerServices;

using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.Core.SourceControl.Models;

namespace Netptune.UnitTests.Netptune.SourceControl.Support;

public sealed class FakeSourceControlProvider : ISourceControlProvider
{
    public SourceControlProvider Provider { get; }

    public SourceControlCapabilities Capabilities =>
        SourceControlCapabilities.ListRepositories | SourceControlCapabilities.Webhooks;

    public ScmTerminology Terminology { get; } = new()
    {
        PullRequest = "Merge request",
        PullRequests = "Merge requests",
        Issue = "Issue",
        CheckRuns = "Pipelines",
    };

    public CountingConnector FakeConnector { get; } = new();

    public ISourceControlConnector Connector => FakeConnector;

    public ISourceControlClient Client { get; } = new FakeClient();

    public ISourceControlWebhookReader? WebhookReader => null;

    public FakeSourceControlProvider(SourceControlProvider provider = SourceControlProvider.GitLab)
    {
        Provider = provider;
    }

    public sealed class CountingConnector : ISourceControlConnector
    {
        public int TokenRequests { get; private set; }

        public ScmConnectStart BeginConnect(ScmConnectRequest request)
        {
            return new ScmConnectStart("https://fake.example/connect");
        }

        public ValueTask<ClientResponse<ScmConnectResult>> CompleteConnect(
            ScmConnectCallback callback,
            CancellationToken cancellationToken)
        {
            var response = ClientResponse<ScmConnectResult>.Failed("Not supported by the fake provider.");

            return ValueTask.FromResult(response);
        }

        public ValueTask<ScmAccessToken> GetAccessToken(ScmConnection connection, CancellationToken cancellationToken)
        {
            TokenRequests++;

            var token = new ScmAccessToken("fake-token", DateTimeOffset.MaxValue);

            return ValueTask.FromResult(token);
        }

        public ValueTask<SourceControlCapabilities> GetGrantedCapabilities(
            ScmConnection connection,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(SourceControlCapabilities.ListRepositories | SourceControlCapabilities.Webhooks);
        }
    }

    private sealed class FakeClient : ISourceControlClient
    {
        public async IAsyncEnumerable<ScmRepository> ListRepositories(
            ScmConnectionContext context,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await context.GetAccessToken(cancellationToken);

            yield return new ScmRepository
            {
                Ref = new ScmRepositoryRef("1", "fake-group", "fake-repo"),
                FullName = "fake-group/fake-repo",
                DefaultBranch = "main",
                WebUrl = "https://fake.example/fake-group/fake-repo",
            };
        }

        public ValueTask<IReadOnlyList<ScmBranch>> ListBranches(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<ScmBranch?> GetBranch(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string name,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyList<ScmCommit>> ListCommits(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            ScmCommitQuery query,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<ScmPullRequest?> GetPullRequest(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string number,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyList<ScmPullRequest>> ListPullRequests(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            ScmPullRequestQuery query,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyList<ScmCheckRun>> ListCheckRuns(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string commitSha,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<ClientResponse<ScmBranch>> CreateBranch(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            ScmCreateBranch request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
