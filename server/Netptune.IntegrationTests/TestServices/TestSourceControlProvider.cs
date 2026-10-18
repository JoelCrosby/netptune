using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Connect;
using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.SourceControl.Webhooks;

namespace Netptune.IntegrationTests.TestServices;

// Registered as GitLab so it never collides with the real GitHub provider, which the test host
// leaves disabled. Accounts named in the connect callback get two repositories each.
public sealed class TestSourceControlProvider : ISourceControlProvider
{
    public const string DeniedCode = "denied";

    public SourceControlProvider Provider => SourceControlProvider.GitLab;

    public const string HeadSha = "1111111111111111111111111111111111111111";
    public const string ReadmeText = "# Test repository\nHello from the test provider.\n";

    public static SourceControlCapabilities Granted =>
        SourceControlCapabilities.ListRepositories
        | SourceControlCapabilities.ReadBranches
        | SourceControlCapabilities.ReadCommits
        | SourceControlCapabilities.ReadPullRequests
        | SourceControlCapabilities.ReadCheckRuns
        | SourceControlCapabilities.Webhooks;

    public SourceControlCapabilities Capabilities => Granted;

    public ScmTerminology Terminology { get; } = new()
    {
        PullRequest = "Merge request",
        PullRequests = "Merge requests",
        Issue = "Issue",
        CheckRuns = "Pipelines",
    };

    public ISourceControlConnector Connector { get; } = new TestConnector();

    public ISourceControlClient Client { get; } = new TestClient();

    // What a history sync reads back, keyed by repository external id, so each test seeds its own repo.
    public static ConcurrentDictionary<string, TestRepoHistory> Histories { get; } = new();

    public ISourceControlWebhookReader? WebhookReader { get; } = new TestWebhookReader();

    public const string SignatureHeader = "X-Test-Signature";
    public const string DeliveryHeader = "X-Test-Delivery";
    public const string EventHeader = "X-Test-Event";
    public const string ValidSignature = "valid";
    public const string UninstallEvent = "uninstall";
    public const string PullRequestEvent = "pull_request";
    public const string CheckRunEvent = "check_run";
    public const string PushEvent = "push";
    public const string BranchCreatedEvent = "create";
    public const string BranchDeletedEvent = "delete";
    public const string FailingEvent = "failing";

    // Signed means the signature header says "valid"; the body names the account. An uninstall event
    // normalises to the connection being revoked, which is enough to drive the whole pipeline.
    private sealed class TestWebhookReader : ISourceControlWebhookReader
    {
        public ScmWebhookVerification Verify(ScmWebhookRequest request, ScmConnection? routedConnection)
        {
            var signature = request.GetHeader(SignatureHeader);

            if (signature is null)
            {
                return ScmWebhookVerification.MissingSignature;
            }

            var isValid = signature == ValidSignature;

            return isValid ? ScmWebhookVerification.Verified : ScmWebhookVerification.InvalidSignature;
        }

        public ScmWebhookEnvelope? ReadEnvelope(ScmWebhookRequest request)
        {
            var deliveryId = request.GetHeader(DeliveryHeader);
            var eventName = request.GetHeader(EventHeader);

            if (deliveryId is null || eventName is null)
            {
                return null;
            }

            using var document = JsonDocument.Parse(request.Body);
            var account = document.RootElement.GetProperty("account").GetString();

            return new ScmWebhookEnvelope
            {
                Provider = SourceControlProvider.GitLab,
                DeliveryId = deliveryId,
                EventName = eventName,
                ExternalAccountId = account,
                ReceivedAt = DateTimeOffset.UtcNow,
                Body = request.Body,
            };
        }

        public IReadOnlyList<ScmEvent> Normalise(ScmWebhookEnvelope envelope, ScmConnection connection)
        {
            if (envelope.EventName == PullRequestEvent)
            {
                return [ReadPullRequest(envelope, connection)];
            }

            if (envelope.EventName == CheckRunEvent)
            {
                return [ReadCheckRun(envelope, connection)];
            }

            if (envelope.EventName == PushEvent)
            {
                return [ReadPush(envelope, connection)];
            }

            if (envelope.EventName is BranchCreatedEvent or BranchDeletedEvent)
            {
                return [ReadBranchChange(envelope, connection)];
            }

            if (envelope.EventName == FailingEvent)
            {
                throw new InvalidOperationException("The test provider could not read this delivery.");
            }

            if (envelope.EventName != UninstallEvent)
            {
                return [];
            }

            return
            [
                new ScmConnectionChanged
                {
                    EventId = Guid.NewGuid(),
                    Provider = SourceControlProvider.GitLab,
                    ConnectionId = connection.Id,
                    WorkspaceId = connection.WorkspaceId,
                    OccurredAt = envelope.ReceivedAt,
                    Action = ScmConnectionAction.Revoked,
                    Repositories = [],
                },
            ];
        }
    }

    // The body names the repository by its external id, plus the pull request's number, title, state
    // and optionally its head commit, when it was last updated, and its author's username and id.
    private static ScmPullRequestChanged ReadPullRequest(ScmWebhookEnvelope envelope, ScmConnection connection)
    {
        using var document = JsonDocument.Parse(envelope.Body);
        var root = document.RootElement;
        var repoId = root.GetProperty("repo").GetString()!;
        var number = root.GetProperty("number").GetString()!;
        var state = Enum.Parse<ScmPullRequestState>(root.GetProperty("state").GetString()!);
        var author = new ScmIdentity
        {
            Provider = SourceControlProvider.GitLab,
            ExternalId = root.TryGetProperty("authorId", out var authorId) ? authorId.GetString() : null,
            Username = root.TryGetProperty("author", out var authorName) ? authorName.GetString()! : "tester",
        };
        var updatedAt = root.TryGetProperty("updatedAt", out var updated)
            ? updated.GetDateTimeOffset()
            : envelope.ReceivedAt;

        return new ScmPullRequestChanged
        {
            EventId = Guid.NewGuid(),
            Provider = SourceControlProvider.GitLab,
            ConnectionId = connection.Id,
            WorkspaceId = connection.WorkspaceId,
            OccurredAt = envelope.ReceivedAt,
            Repository = new ScmRepositoryRef(repoId, connection.ExternalAccountId, repoId),
            Action = state == ScmPullRequestState.Merged ? ScmPullRequestAction.Merged : ScmPullRequestAction.Opened,
            PullRequest = new ScmPullRequest
            {
                ExternalId = number,
                Number = number,
                Title = root.GetProperty("title").GetString()!,
                State = state,
                SourceBranch = "feature/test",
                HeadSha = root.TryGetProperty("sha", out var sha) ? sha.GetString() : null,
                TargetBranch = "main",
                Author = author,
                WebUrl = $"https://scm.test/{repoId}/pulls/{number}",
                CreatedAt = envelope.ReceivedAt,
                UpdatedAt = updatedAt,
            },
        };
    }

    // The body names the repository, the branch, and each commit's sha, message and optional author email.
    private static ScmCommitsPushed ReadPush(ScmWebhookEnvelope envelope, ScmConnection connection)
    {
        using var document = JsonDocument.Parse(envelope.Body);
        var root = document.RootElement;
        var repoId = root.GetProperty("repo").GetString()!;
        var commits = root.GetProperty("commits")
            .EnumerateArray()
            .Select(commit => new ScmCommit
            {
                Sha = commit.GetProperty("sha").GetString()!,
                Message = commit.GetProperty("message").GetString()!,
                Author = new ScmIdentity
                {
                    Provider = SourceControlProvider.GitLab,
                    Username = "tester",
                    Email = commit.TryGetProperty("email", out var email) ? email.GetString() : null,
                },
                AuthoredAt = envelope.ReceivedAt,
                WebUrl = $"https://scm.test/{repoId}/commit/{commit.GetProperty("sha").GetString()}",
            })
            .ToList();

        return new ScmCommitsPushed
        {
            EventId = Guid.NewGuid(),
            Provider = SourceControlProvider.GitLab,
            ConnectionId = connection.Id,
            WorkspaceId = connection.WorkspaceId,
            OccurredAt = envelope.ReceivedAt,
            Repository = new ScmRepositoryRef(repoId, connection.ExternalAccountId, repoId),
            Branch = root.GetProperty("branch").GetString()!,
            Commits = commits,
        };
    }

    // The body names the repository and the branch; the event name says whether it was created or deleted.
    private static ScmEvent ReadBranchChange(ScmWebhookEnvelope envelope, ScmConnection connection)
    {
        using var document = JsonDocument.Parse(envelope.Body);
        var root = document.RootElement;
        var repoId = root.GetProperty("repo").GetString()!;
        var branch = root.GetProperty("branch").GetString()!;
        var repository = new ScmRepositoryRef(repoId, connection.ExternalAccountId, repoId);

        if (envelope.EventName == BranchDeletedEvent)
        {
            return new ScmBranchDeleted
            {
                EventId = Guid.NewGuid(),
                Provider = SourceControlProvider.GitLab,
                ConnectionId = connection.Id,
                WorkspaceId = connection.WorkspaceId,
                OccurredAt = envelope.ReceivedAt,
                Repository = repository,
                Branch = branch,
            };
        }

        return new ScmBranchCreated
        {
            EventId = Guid.NewGuid(),
            Provider = SourceControlProvider.GitLab,
            ConnectionId = connection.Id,
            WorkspaceId = connection.WorkspaceId,
            OccurredAt = envelope.ReceivedAt,
            Repository = repository,
            Branch = branch,
            WebUrl = $"https://scm.test/{repoId}/tree/{branch}",
        };
    }

    // The body names the repository, the run's id, name and commit, its status and optional conclusion.
    private static ScmCheckRunChanged ReadCheckRun(ScmWebhookEnvelope envelope, ScmConnection connection)
    {
        using var document = JsonDocument.Parse(envelope.Body);
        var root = document.RootElement;
        var repoId = root.GetProperty("repo").GetString()!;
        var runId = root.GetProperty("id").GetString()!;
        var conclusion = root.TryGetProperty("conclusion", out var value)
            ? Enum.Parse<ScmCheckRunConclusion>(value.GetString()!)
            : (ScmCheckRunConclusion?)null;

        return new ScmCheckRunChanged
        {
            EventId = Guid.NewGuid(),
            Provider = SourceControlProvider.GitLab,
            ConnectionId = connection.Id,
            WorkspaceId = connection.WorkspaceId,
            OccurredAt = envelope.ReceivedAt,
            Repository = new ScmRepositoryRef(repoId, connection.ExternalAccountId, repoId),
            CheckRun = new ScmCheckRun
            {
                ExternalId = runId,
                Name = root.GetProperty("name").GetString()!,
                Status = Enum.Parse<ScmCheckRunStatus>(root.GetProperty("status").GetString()!),
                Conclusion = conclusion,
                CommitSha = root.GetProperty("sha").GetString()!,
                WebUrl = $"https://scm.test/{repoId}/runs/{runId}",
            },
            PullRequestNumbers = [],
        };
    }

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
                GrantedCapabilities = Granted,
            };

            return ValueTask.FromResult(ClientResponse<ScmConnectResult>.Success(result));
        }

        public ValueTask<ScmAccessToken> GetAccessToken(ScmConnection connection, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new ScmAccessToken("test-token", DateTimeOffset.MaxValue));
        }

        public ValueTask<SourceControlCapabilities> GetGrantedCapabilities(
            ScmConnection connection,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(Granted);
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
            IReadOnlyList<ScmBranch> branches = HistoryOf(repository).Branches;

            return ValueTask.FromResult(branches);
        }

        public ValueTask<IReadOnlyList<ScmCommit>> CompareCommits(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string baseBranch,
            string headBranch,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ScmCommit> commits = HistoryOf(repository).BranchCommits.GetValueOrDefault(headBranch) ?? [];

            return ValueTask.FromResult(commits);
        }

        public ValueTask<ScmBranch?> GetBranch(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string name,
            CancellationToken cancellationToken)
        {
            var branch = name == "main" ? new ScmBranch("main", HeadSha, "https://scm.test/tree/main") : null;

            return ValueTask.FromResult(branch);
        }

        public ValueTask<IReadOnlyList<ScmCommit>> ListCommits(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            ScmCommitQuery query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ScmCommit> commits =
            [
                new ScmCommit
                {
                    Sha = HeadSha,
                    Message = "Add the readme\n\nWith a body.",
                    Author = new ScmIdentity { Provider = SourceControlProvider.GitLab, Username = "tester" },
                    WebUrl = $"https://scm.test/commit/{HeadSha}",
                },
            ];

            return ValueTask.FromResult(commits);
        }

        public ValueTask<ScmPullRequest?> GetPullRequest(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string number,
            CancellationToken cancellationToken)
        {
            var pullRequest = number == "7"
                ? new ScmPullRequest
                {
                    ExternalId = "7",
                    Number = "7",
                    Title = "Add the readme",
                    State = ScmPullRequestState.Open,
                    SourceBranch = "feature/readme",
                    TargetBranch = "main",
                    Author = new ScmIdentity { Provider = SourceControlProvider.GitLab, Username = "tester" },
                    WebUrl = "https://scm.test/pulls/7",
                }
                : null;

            return ValueTask.FromResult(pullRequest);
        }

        public ValueTask<IReadOnlyList<ScmPullRequest>> ListPullRequests(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            ScmPullRequestQuery query,
            CancellationToken cancellationToken)
        {
            var history = HistoryOf(repository);

            if (history.FailsWithStatus is { } status)
            {
                throw new ScmProviderException(SourceControlProvider.GitLab, status, "history unavailable");
            }

            IReadOnlyList<ScmPullRequest> pullRequests = history.PullRequests
                .Where(pullRequest => query.IncludeClosed || IsOpen(pullRequest))
                .Where(pullRequest => query.UpdatedSince is null || pullRequest.UpdatedAt >= query.UpdatedSince)
                .Take(query.Take)
                .ToList();

            return ValueTask.FromResult(pullRequests);
        }

        public ValueTask<ScmTree> GetTree(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string commitSha,
            CancellationToken cancellationToken)
        {
            var tree = new ScmTree
            {
                CommitSha = commitSha,
                Entries =
                [
                    new ScmTreeEntry("README.md", false, ReadmeText.Length),
                    new ScmTreeEntry("src", true, null),
                    new ScmTreeEntry("src/app.ts", false, 20),
                ],
            };

            return ValueTask.FromResult(tree);
        }

        public ValueTask<ScmFileContent?> GetFile(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string path,
            string commitSha,
            CancellationToken cancellationToken)
        {
            var file = path == "README.md"
                ? new ScmFileContent
                {
                    Path = path,
                    Size = ReadmeText.Length,
                    Text = ReadmeText,
                    WebUrl = "https://scm.test/README.md",
                }
                : null;

            return ValueTask.FromResult(file);
        }

        public ValueTask<IReadOnlyList<ScmCodeMatch>> SearchCode(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string query,
            CancellationToken cancellationToken)
        {
            var match = new ScmCodeMatch("README.md", ["Hello from the test provider."], "https://scm.test/README.md");
            IReadOnlyList<ScmCodeMatch> matches = [match];

            return ValueTask.FromResult(matches);
        }

        public ValueTask<IReadOnlyList<ScmPullRequestFile>> ListPullRequestFiles(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string number,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ScmPullRequestFile> files =
            [
                new ScmPullRequestFile
                {
                    Path = "README.md",
                    Status = "added",
                    Additions = 2,
                    Patch = "+# Test repository",
                },
            ];

            return ValueTask.FromResult(files);
        }

        public ValueTask<IReadOnlyList<ScmCheckRun>> ListCheckRuns(
            ScmConnectionContext context,
            ScmRepositoryRef repository,
            string commitSha,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ScmCheckRun> checkRuns = HistoryOf(repository).CheckRuns.GetValueOrDefault(commitSha) ?? [];

            return ValueTask.FromResult(checkRuns);
        }

        private static bool IsOpen(ScmPullRequest pullRequest)
        {
            return pullRequest.State is ScmPullRequestState.Open or ScmPullRequestState.Draft;
        }

        private static TestRepoHistory HistoryOf(ScmRepositoryRef repository)
        {
            return Histories.GetValueOrDefault(repository.ExternalId) ?? new TestRepoHistory();
        }
    }
}

public sealed class TestRepoHistory
{
    public List<ScmPullRequest> PullRequests { get; } = [];

    public List<ScmBranch> Branches { get; } = [];

    public Dictionary<string, List<ScmCommit>> BranchCommits { get; } = [];

    public Dictionary<string, List<ScmCheckRun>> CheckRuns { get; } = [];

    public int? FailsWithStatus { get; init; }
}
