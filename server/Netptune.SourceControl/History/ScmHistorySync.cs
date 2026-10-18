using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Services.Realtime;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.History;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.UnitOfWork;
using Netptune.SourceControl.Webhooks;

namespace Netptune.SourceControl.History;

// Replays a linked repository's recent history through the webhook handlers, so links follow the same rules
// as live events. Nothing is recorded as activity and no automation runs: history is what happened while
// Netptune was watching.
internal sealed class ScmHistorySync : IScmHistorySync
{
    private static readonly TimeSpan HistoryWindow = TimeSpan.FromDays(90);

    private const int MaxOpenPullRequests = 100;
    private const int MaxRecentPullRequests = 200;
    private const int MaxBranchCommits = 100;
    private const int MaxErrorLength = 512;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly IEnumerable<IScmEventHandler> Handlers;
    private readonly IWorkspaceEventPublisher WorkspaceEvents;
    private readonly TimeProvider Clock;
    private readonly ILogger<ScmHistorySync> Logger;

    public ScmHistorySync(
        INetptuneUnitOfWork unitOfWork,
        ISourceControlProviderRegistry registry,
        IEnumerable<IScmEventHandler> handlers,
        IWorkspaceEventPublisher workspaceEvents,
        TimeProvider clock,
        ILogger<ScmHistorySync> logger)
    {
        UnitOfWork = unitOfWork;
        Registry = registry;
        Handlers = handlers;
        WorkspaceEvents = workspaceEvents;
        Clock = clock;
        Logger = logger;
    }

    private sealed record SyncTarget(
        SourceControlRepo Repo,
        ISourceControlClient Client,
        ScmConnectionContext Context,
        SourceControlCapabilities Capabilities);

    // The claim comes first: a sync can outlast the broker's ack wait, and the redelivery that follows must
    // not start a second one alongside it. Loading the repo after claiming also lets the release below save.
    public async Task Sync(int repoId, CancellationToken cancellationToken)
    {
        var now = Clock.GetUtcNow().UtcDateTime;
        var claimed = await UnitOfWork.SourceControlRepos.TryClaimHistorySync(repoId, now, cancellationToken);

        if (!claimed)
        {
            Logger.LogInformation("History sync of source control repo {RepoId} is already running", repoId);

            return;
        }

        try
        {
            await SyncClaimed(repoId, cancellationToken);
        }
        catch
        {
            await UnitOfWork.SourceControlRepos.ReleaseHistorySync(repoId, CancellationToken.None);

            throw;
        }
    }

    private async Task SyncClaimed(int repoId, CancellationToken cancellationToken)
    {
        var repo = await UnitOfWork.SourceControlRepos.GetForHistorySync(repoId, cancellationToken);
        var isSyncable = repo is { IsLinked: true, IsAccessible: true }
            && repo.Connection.Status == ScmConnectionStatus.Active
            && Registry.IsEnabled(repo.Connection.Provider);

        if (!isSyncable)
        {
            await UnitOfWork.SourceControlRepos.ReleaseHistorySync(repoId, cancellationToken);

            return;
        }

        var target = CreateTarget(repo!);

        try
        {
            await SyncPullRequestsAndBranches(target, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            var isStillLinked = await UnitOfWork.SourceControlRepos.IsLinked(repoId, cancellationToken);

            if (isStillLinked)
            {
                await SyncChecks(target, cancellationToken);

                repo!.HistorySyncedAt = Clock.GetUtcNow().UtcDateTime;
                repo.HistorySyncError = null;
            }
        }
        catch (Exception exception) when (exception is ScmProviderException or ScmRateLimitedException)
        {
            Logger.LogWarning(exception, "Syncing the history of source control repo {RepoId} failed", repoId);

            repo!.HistorySyncError = exception.Message.Length > MaxErrorLength
                ? exception.Message[..MaxErrorLength]
                : exception.Message;
        }

        repo!.HistorySyncStartedAt = null;

        await UnitOfWork.CompleteAsync(cancellationToken);
        await NotifyWorkspace(repo);
    }

    private SyncTarget CreateTarget(SourceControlRepo repo)
    {
        var connection = repo.Connection.ToScmConnection();
        var provider = Registry.Resolve(repo.Connection.Provider);
        var context = Registry.CreateContext(connection);
        var capabilities = Registry.GetEffectiveCapabilities(connection);

        return new SyncTarget(repo, provider.Client, context, capabilities);
    }

    private async Task SyncPullRequestsAndBranches(SyncTarget target, CancellationToken cancellationToken)
    {
        var since = Clock.GetUtcNow() - HistoryWindow;
        var projectKeys = await UnitOfWork.Projects.GetProjectKeys(target.Repo.WorkspaceId, cancellationToken);
        var extractor = new TaskReferenceExtractor(projectKeys);
        var canReadPullRequests = target.Capabilities.HasFlag(SourceControlCapabilities.ReadPullRequests);
        var canReadBranches = target.Capabilities.HasFlag(
            SourceControlCapabilities.ReadBranches | SourceControlCapabilities.ReadCommits);

        if (canReadPullRequests)
        {
            var pullRequests = await ListPullRequests(target, since, cancellationToken);

            foreach (var pullRequest in pullRequests)
            {
                await Dispatch(target, PullRequestEvent(target.Repo, pullRequest), cancellationToken);
            }
        }

        if (!canReadBranches)
        {
            return;
        }

        var repository = target.Repo.ToRepositoryRef();
        var branches = await target.Client.ListBranches(target.Context, repository, cancellationToken);
        var taskBranches = branches
            .Where(branch => branch.Name != target.Repo.DefaultBranch)
            .Where(branch => extractor.Extract(branch.Name).Count > 0)
            .ToList();

        foreach (var branch in taskBranches)
        {
            await SyncBranch(target, branch, since, cancellationToken);
        }
    }

    private async Task<List<ScmPullRequest>> ListPullRequests(
        SyncTarget target,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        var repository = target.Repo.ToRepositoryRef();
        var openQuery = new ScmPullRequestQuery(null, IncludeClosed: false, MaxOpenPullRequests);
        var recentQuery = new ScmPullRequestQuery(null, IncludeClosed: true, MaxRecentPullRequests, since);

        var open = await target.Client.ListPullRequests(target.Context, repository, openQuery, cancellationToken);
        var recent = await target.Client.ListPullRequests(target.Context, repository, recentQuery, cancellationToken);

        return open
            .Concat(recent)
            .DistinctBy(pullRequest => pullRequest.Number)
            .ToList();
    }

    private async Task SyncBranch(
        SyncTarget target,
        ScmBranch branch,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        var repository = target.Repo.ToRepositoryRef();
        var branchCommits = await target.Client.CompareCommits(
            target.Context,
            repository,
            target.Repo.DefaultBranch,
            branch.Name,
            cancellationToken);
        var recentCommits = branchCommits
            .Where(commit => commit.AuthoredAt >= since)
            .TakeLast(MaxBranchCommits)
            .ToList();
        var lastCommitAt = recentCommits.Count > 0 ? recentCommits[^1].AuthoredAt : Clock.GetUtcNow();

        await Dispatch(target, BranchEvent(target.Repo, branch, lastCommitAt), cancellationToken);

        if (recentCommits.Count == 0)
        {
            return;
        }

        await Dispatch(target, CommitsEvent(target.Repo, branch, recentCommits, lastCommitAt), cancellationToken);
    }

    private async Task SyncChecks(SyncTarget target, CancellationToken cancellationToken)
    {
        var canReadChecks = target.Capabilities.HasFlag(SourceControlCapabilities.ReadCheckRuns);

        if (!canReadChecks)
        {
            return;
        }

        var heads = await UnitOfWork.TaskCodeLinks.GetOpenPullRequestHeads(target.Repo.Id, cancellationToken);
        var repository = target.Repo.ToRepositoryRef();

        foreach (var head in heads)
        {
            var checkRuns = await target.Client.ListCheckRuns(
                target.Context,
                repository,
                head.HeadSha,
                cancellationToken);

            // Saved one at a time: each run's summary is read back from the stored runs for its commit.
            foreach (var checkRun in checkRuns)
            {
                var checkRunEvent = CheckRunEvent(target.Repo, checkRun, head.PullRequestNumber);

                await Dispatch(target, checkRunEvent, cancellationToken);
                await UnitOfWork.CompleteAsync(cancellationToken);
            }
        }
    }

    private async Task Dispatch(SyncTarget target, ScmEvent scmEvent, CancellationToken cancellationToken)
    {
        var handler = Handlers.First(candidate => candidate.CanHandle(scmEvent));

        await handler.Handle(scmEvent, target.Repo.Connection, ScmEventSource.HistorySync, cancellationToken);
    }

    private async Task NotifyWorkspace(SourceControlRepo repo)
    {
        try
        {
            string[] scopes = [WorkspaceEventScopes.SourceControl, WorkspaceEventScopes.CodeLinks];

            await WorkspaceEvents.PublishAsync(repo.Workspace.Slug, string.Empty, scopes);
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Publishing the history sync of repo {RepoId} failed", repo.Id);
        }
    }

    private static ScmPullRequestChanged PullRequestEvent(SourceControlRepo repo, ScmPullRequest pullRequest)
    {
        return new ScmPullRequestChanged
        {
            EventId = Guid.NewGuid(),
            Provider = repo.Connection.Provider,
            ConnectionId = repo.ConnectionId,
            WorkspaceId = repo.WorkspaceId,
            OccurredAt = pullRequest.UpdatedAt,
            Repository = repo.ToRepositoryRef(),
            PullRequest = pullRequest,
            Action = ScmPullRequestAction.Edited,
        };
    }

    private static ScmBranchCreated BranchEvent(SourceControlRepo repo, ScmBranch branch, DateTimeOffset occurredAt)
    {
        return new ScmBranchCreated
        {
            EventId = Guid.NewGuid(),
            Provider = repo.Connection.Provider,
            ConnectionId = repo.ConnectionId,
            WorkspaceId = repo.WorkspaceId,
            OccurredAt = occurredAt,
            Repository = repo.ToRepositoryRef(),
            Branch = branch.Name,
            WebUrl = branch.WebUrl,
        };
    }

    private static ScmCommitsPushed CommitsEvent(
        SourceControlRepo repo,
        ScmBranch branch,
        List<ScmCommit> commits,
        DateTimeOffset occurredAt)
    {
        return new ScmCommitsPushed
        {
            EventId = Guid.NewGuid(),
            Provider = repo.Connection.Provider,
            ConnectionId = repo.ConnectionId,
            WorkspaceId = repo.WorkspaceId,
            OccurredAt = occurredAt,
            Repository = repo.ToRepositoryRef(),
            Branch = branch.Name,
            Commits = commits,
        };
    }

    private ScmCheckRunChanged CheckRunEvent(SourceControlRepo repo, ScmCheckRun checkRun, string number)
    {
        return new ScmCheckRunChanged
        {
            EventId = Guid.NewGuid(),
            Provider = repo.Connection.Provider,
            ConnectionId = repo.ConnectionId,
            WorkspaceId = repo.WorkspaceId,
            OccurredAt = checkRun.CompletedAt ?? Clock.GetUtcNow(),
            Repository = repo.ToRepositoryRef(),
            CheckRun = checkRun,
            PullRequestNumbers = [number],
        };
    }
}
