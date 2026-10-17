using System.Runtime.CompilerServices;

using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.Core.SourceControl.Models;

namespace Netptune.IntegrationTests.TestServices;

// Registered as GitLab so it never collides with the real GitHub provider, which the test host
// leaves disabled. Accounts named in the connect callback get two repositories each.
public sealed class TestSourceControlProvider : ISourceControlProvider
{
    public const string DeniedCode = "denied";

    public SourceControlProvider Provider => SourceControlProvider.GitLab;

    public SourceControlCapabilities Capabilities =>
        SourceControlCapabilities.ListRepositories | SourceControlCapabilities.Webhooks;

    public ScmTerminology Terminology { get; } = new()
    {
        PullRequest = "Merge request",
        PullRequests = "Merge requests",
        Issue = "Issue",
        CheckRuns = "Pipelines",
    };

    public ISourceControlConnector Connector { get; } = new TestConnector();

    public ISourceControlClient Client { get; } = new TestClient();

    public ISourceControlWebhookReader? WebhookReader => null;

    private sealed class TestConnector : ISourceControlConnector
    {
        public ScmConnectStart BeginConnect(ScmConnectRequest request)
        {
            return new ScmConnectStart($"https://scm.test/connect?workspace={request.WorkspaceId}");
        }

        public ValueTask<ClientResponse<ScmConnectResult>> CompleteConnect(
            ScmConnectCallback callback,
            CancellationToken cancellationToken)
        {
            var account = callback.Parameters.GetValueOrDefault("account");
            var isDenied = callback.Parameters.GetValueOrDefault("code") == DeniedCode;

            if (account is null || isDenied)
            {
                var denied = ClientResponse<ScmConnectResult>.Failed("The test provider denied the connection.");

                return ValueTask.FromResult(denied);
            }

            var result = new ScmConnectResult
            {
                ExternalAccountId = account,
                Account = new ScmAccount { ExternalId = account, Login = account, AccountType = ScmAccountType.Group },
                CredentialKind = ScmCredentialKind.PersonalAccessToken,
                GrantedCapabilities = SourceControlCapabilities.ListRepositories | SourceControlCapabilities.Webhooks,
            };

            return ValueTask.FromResult(ClientResponse<ScmConnectResult>.Success(result));
        }

        public ValueTask<ScmAccessToken> GetAccessToken(ScmConnection connection, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new ScmAccessToken("test-token", DateTimeOffset.MaxValue));
        }
    }

    private sealed class TestClient : ISourceControlClient
    {
        public async IAsyncEnumerable<ScmRepository> ListRepositories(
            ScmConnectionContext context,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var account = context.Connection.ExternalAccountId;

            foreach (var name in new[] { "api", "web" })
            {
                yield return new ScmRepository
                {
                    Ref = new ScmRepositoryRef($"{account}-{name}", account, name),
                    FullName = $"{account}/{name}",
                    DefaultBranch = "main",
                    WebUrl = $"https://scm.test/{account}/{name}",
                };
            }
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
