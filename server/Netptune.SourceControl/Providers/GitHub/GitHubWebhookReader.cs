using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.SourceControl.Providers.GitHub.Dtos;

namespace Netptune.SourceControl.Providers.GitHub;

internal sealed class GitHubWebhookReader : ISourceControlWebhookReader
{
    private const string SignatureHeader = "X-Hub-Signature-256";
    private const string SignaturePrefix = "sha256=";
    private const string DeliveryHeader = "X-GitHub-Delivery";
    private const string EventHeader = "X-GitHub-Event";
    private const string BranchRefPrefix = "refs/heads/";

    private readonly GitHubAppOptions Options;
    private readonly TimeProvider Clock;

    public GitHubWebhookReader(IOptions<GitHubAppOptions> options, TimeProvider clock)
    {
        Options = options.Value;
        Clock = clock;
    }

    public ScmWebhookVerification Verify(ScmWebhookRequest request, ScmConnection? routedConnection)
    {
        if (string.IsNullOrEmpty(Options.WebhookSecret))
        {
            return ScmWebhookVerification.NotConfigured;
        }

        var signature = request.GetHeader(SignatureHeader);
        var hasSignature = signature is not null && signature.StartsWith(SignaturePrefix, StringComparison.Ordinal);

        if (!hasSignature)
        {
            return ScmWebhookVerification.MissingSignature;
        }

        var providedHex = signature![SignaturePrefix.Length..];
        var provided = TryDecodeHex(providedHex);

        if (provided is null)
        {
            return ScmWebhookVerification.InvalidSignature;
        }

        var secret = Encoding.UTF8.GetBytes(Options.WebhookSecret);
        var expected = HMACSHA256.HashData(secret, request.Body.Span);
        var matches = CryptographicOperations.FixedTimeEquals(expected, provided);

        return matches ? ScmWebhookVerification.Verified : ScmWebhookVerification.InvalidSignature;
    }

    public ScmWebhookEnvelope? ReadEnvelope(ScmWebhookRequest request)
    {
        var deliveryId = request.GetHeader(DeliveryHeader);
        var eventName = request.GetHeader(EventHeader);
        var hasRoutingHeaders = !string.IsNullOrWhiteSpace(deliveryId) && !string.IsNullOrWhiteSpace(eventName);

        if (!hasRoutingHeaders)
        {
            return null;
        }

        var envelope = TryDeserialize<GitHubEnvelopeDto>(request.Body);

        if (envelope is null)
        {
            return null;
        }

        var installationId = envelope.Installation is null ? null : GitHubMapper.ToExternalId(envelope.Installation.Id);

        return new ScmWebhookEnvelope
        {
            Provider = SourceControlProvider.GitHub,
            DeliveryId = deliveryId!,
            EventName = eventName!,
            Action = envelope.Action,
            ExternalAccountId = installationId,
            ReceivedAt = Clock.GetUtcNow(),
            Body = request.Body,
        };
    }

    public IReadOnlyList<ScmEvent> Normalise(ScmWebhookEnvelope envelope, ScmConnection connection)
    {
        var eventId = ScmEventIds.Create(SourceControlProvider.GitHub, envelope.DeliveryId, 0);
        var scope = new NormaliseScope(envelope, connection, eventId);

        var normalised = envelope.EventName switch
        {
            "push" => NormalisePush(scope),
            "create" => NormaliseBranchCreated(scope),
            "delete" => NormaliseBranchDeleted(scope),
            "pull_request" => NormalisePullRequest(scope),
            "check_run" => NormaliseCheckRun(scope),
            "installation" => NormaliseInstallation(scope),
            "installation_repositories" => NormaliseInstallationRepositories(scope),
            _ => null,
        };

        return normalised is null ? [] : [normalised];
    }

    private static ScmEvent? NormalisePush(NormaliseScope scope)
    {
        var payload = Deserialize<GitHubPushPayload>(scope.Envelope.Body);

        if (payload is null)
        {
            return null;
        }

        var isBranch = payload.Ref.StartsWith(BranchRefPrefix, StringComparison.Ordinal);
        var hasCommits = payload.Commits.Count > 0;

        // Branch creation and deletion arrive as their own create/delete events, so a push only
        // matters when it carries commits.
        var isCommitPush = isBranch && !payload.Deleted && hasCommits;

        if (!isCommitPush)
        {
            return null;
        }

        var commits = payload.Commits.Select(GitHubMapper.ToCommit).ToList();
        var occurredAt = payload.Commits[^1].Timestamp;

        return new ScmCommitsPushed
        {
            EventId = scope.EventId,
            Provider = SourceControlProvider.GitHub,
            ConnectionId = scope.Connection.Id,
            WorkspaceId = scope.Connection.WorkspaceId,
            OccurredAt = occurredAt,
            Actor = ToActor(payload.Sender),
            Repository = GitHubMapper.ToRepositoryRef(payload.Repository),
            Branch = payload.Ref[BranchRefPrefix.Length..],
            Commits = commits,
            Forced = payload.Forced,
        };
    }

    private ScmEvent? NormaliseBranchCreated(NormaliseScope scope)
    {
        var payload = Deserialize<GitHubRefPayload>(scope.Envelope.Body);
        var isBranch = payload?.RefType == "branch";

        if (!isBranch)
        {
            return null;
        }

        var repository = GitHubMapper.ToRepositoryRef(payload!.Repository);

        return new ScmBranchCreated
        {
            EventId = scope.EventId,
            Provider = SourceControlProvider.GitHub,
            ConnectionId = scope.Connection.Id,
            WorkspaceId = scope.Connection.WorkspaceId,
            OccurredAt = scope.Envelope.ReceivedAt,
            Actor = ToActor(payload.Sender),
            Repository = repository,
            Branch = payload.Ref,
            WebUrl = GitHubUrls.Branch(Options.WebBaseUrl, repository, payload.Ref),
        };
    }

    private static ScmEvent? NormaliseBranchDeleted(NormaliseScope scope)
    {
        var payload = Deserialize<GitHubRefPayload>(scope.Envelope.Body);
        var isBranch = payload?.RefType == "branch";

        if (!isBranch)
        {
            return null;
        }

        return new ScmBranchDeleted
        {
            EventId = scope.EventId,
            Provider = SourceControlProvider.GitHub,
            ConnectionId = scope.Connection.Id,
            WorkspaceId = scope.Connection.WorkspaceId,
            OccurredAt = scope.Envelope.ReceivedAt,
            Actor = ToActor(payload!.Sender),
            Repository = GitHubMapper.ToRepositoryRef(payload.Repository),
            Branch = payload.Ref,
        };
    }

    private static ScmEvent? NormalisePullRequest(NormaliseScope scope)
    {
        var payload = Deserialize<GitHubPullRequestPayload>(scope.Envelope.Body);

        if (payload is null)
        {
            return null;
        }

        var action = ToPullRequestAction(payload);

        if (action is null)
        {
            return null;
        }

        return new ScmPullRequestChanged
        {
            EventId = scope.EventId,
            Provider = SourceControlProvider.GitHub,
            ConnectionId = scope.Connection.Id,
            WorkspaceId = scope.Connection.WorkspaceId,
            OccurredAt = payload.PullRequest.UpdatedAt,
            Actor = ToActor(payload.Sender),
            Repository = GitHubMapper.ToRepositoryRef(payload.Repository),
            PullRequest = GitHubMapper.ToPullRequest(payload.PullRequest),
            Action = action.Value,
        };
    }

    private static ScmEvent? NormaliseCheckRun(NormaliseScope scope)
    {
        var payload = Deserialize<GitHubCheckRunPayload>(scope.Envelope.Body);
        var isCompleted = payload?.Action == "completed";

        if (!isCompleted)
        {
            return null;
        }

        var checkRun = payload!.CheckRun;
        var linkedPullRequests = checkRun.PullRequests ?? [];

        var pullRequestNumbers = linkedPullRequests
            .Select(pullRequest => pullRequest.Number.ToString(CultureInfo.InvariantCulture))
            .ToList();

        return new ScmCheckRunCompleted
        {
            EventId = scope.EventId,
            Provider = SourceControlProvider.GitHub,
            ConnectionId = scope.Connection.Id,
            WorkspaceId = scope.Connection.WorkspaceId,
            OccurredAt = checkRun.CompletedAt ?? scope.Envelope.ReceivedAt,
            Actor = ToActor(payload.Sender),
            Repository = GitHubMapper.ToRepositoryRef(payload.Repository),
            CheckRun = GitHubMapper.ToCheckRun(checkRun),
            PullRequestNumbers = pullRequestNumbers,
        };
    }

    private static ScmEvent? NormaliseInstallation(NormaliseScope scope)
    {
        var payload = Deserialize<GitHubInstallationPayload>(scope.Envelope.Body);

        ScmConnectionAction? action = payload?.Action switch
        {
            "deleted" => ScmConnectionAction.Revoked,
            "suspend" => ScmConnectionAction.Suspended,
            "unsuspend" => ScmConnectionAction.Unsuspended,
            "new_permissions_accepted" => ScmConnectionAction.PermissionsChanged,
            _ => null,
        };

        if (action is null)
        {
            return null;
        }

        return CreateConnectionChanged(scope, action.Value, payload!.Sender, []);
    }

    private static ScmEvent? NormaliseInstallationRepositories(NormaliseScope scope)
    {
        var payload = Deserialize<GitHubInstallationRepositoriesPayload>(scope.Envelope.Body);

        if (payload is null)
        {
            return null;
        }

        var isAdded = payload.Action == "added";
        var isRemoved = payload.Action == "removed";

        if (isAdded)
        {
            var added = payload.RepositoriesAdded.Select(GitHubMapper.ToRepositoryRef).ToList();

            return CreateConnectionChanged(scope, ScmConnectionAction.RepositoriesAdded, payload.Sender, added);
        }

        if (isRemoved)
        {
            var removed = payload.RepositoriesRemoved.Select(GitHubMapper.ToRepositoryRef).ToList();

            return CreateConnectionChanged(scope, ScmConnectionAction.RepositoriesRemoved, payload.Sender, removed);
        }

        return null;
    }

    private static ScmConnectionChanged CreateConnectionChanged(
        NormaliseScope scope,
        ScmConnectionAction action,
        GitHubAccountDto? sender,
        IReadOnlyList<ScmRepositoryRef> repositories)
    {
        return new ScmConnectionChanged
        {
            EventId = scope.EventId,
            Provider = SourceControlProvider.GitHub,
            ConnectionId = scope.Connection.Id,
            WorkspaceId = scope.Connection.WorkspaceId,
            OccurredAt = scope.Envelope.ReceivedAt,
            Actor = ToActor(sender),
            Action = action,
            Repositories = repositories,
        };
    }

    private static ScmPullRequestAction? ToPullRequestAction(GitHubPullRequestPayload payload)
    {
        var isMerged = payload.PullRequest.Merged == true || payload.PullRequest.MergedAt is not null;

        return payload.Action switch
        {
            "opened" => ScmPullRequestAction.Opened,
            "edited" => ScmPullRequestAction.Edited,
            "synchronize" => ScmPullRequestAction.Synchronized,
            "ready_for_review" => ScmPullRequestAction.ReadyForReview,
            "converted_to_draft" => ScmPullRequestAction.ConvertedToDraft,
            "reopened" => ScmPullRequestAction.Reopened,
            "closed" when isMerged => ScmPullRequestAction.Merged,
            "closed" => ScmPullRequestAction.Closed,
            _ => null,
        };
    }

    private static ScmIdentity? ToActor(GitHubAccountDto? sender)
    {
        return sender is null ? null : GitHubMapper.ToIdentity(sender);
    }

    // Normalise only sees bodies that passed signature verification, so a payload that doesn't parse
    // is a contract change worth failing the job over rather than dropping quietly.
    private static T? Deserialize<T>(ReadOnlyMemory<byte> body) where T : class
    {
        return JsonSerializer.Deserialize<T>(body.Span, GitHubApi.JsonOptions);
    }

    private static T? TryDeserialize<T>(ReadOnlyMemory<byte> body) where T : class
    {
        try
        {
            return Deserialize<T>(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static byte[]? TryDecodeHex(string hex)
    {
        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private sealed record NormaliseScope(ScmWebhookEnvelope Envelope, ScmConnection Connection, Guid EventId);
}
