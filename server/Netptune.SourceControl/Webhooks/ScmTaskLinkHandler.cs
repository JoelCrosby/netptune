using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.SourceControl;
using Netptune.Core.Services.Realtime;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.UnitOfWork;

namespace Netptune.SourceControl.Webhooks;

internal sealed class ScmTaskLinkHandler : IScmEventHandler
{
    private const int MaxTitleLength = 512;
    private const int MaxExternalRefLength = 256;
    private const int MaxWebUrlLength = 1024;
    private const int MaxAuthorLength = 256;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ScmTaskActivityRecorder Activity;
    private readonly ScmAutomationEvents Automation;
    private readonly TimeProvider Clock;

    public ScmTaskLinkHandler(
        INetptuneUnitOfWork unitOfWork,
        ScmTaskActivityRecorder activity,
        ScmAutomationEvents automation,
        TimeProvider clock)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        Automation = automation;
        Clock = clock;
    }

    public string RefreshScope => WorkspaceEventScopes.CodeLinks;

    public bool CanHandle(ScmEvent scmEvent)
    {
        return scmEvent is ScmBranchCreated or ScmBranchDeleted or ScmCommitsPushed or ScmPullRequestChanged;
    }

    public async Task<bool> Handle(
        ScmEvent scmEvent,
        SourceControlConnection connection,
        ScmEventSource source,
        CancellationToken cancellationToken)
    {
        var repository = RepositoryOf(scmEvent);
        var repo = await UnitOfWork.SourceControlRepos.GetByExternalId(
            connection.Id,
            repository.ExternalId,
            cancellationToken);

        var isTracked = repo is { IsLinked: true, IsAccessible: true };

        if (!isTracked)
        {
            return false;
        }

        if (scmEvent is ScmBranchDeleted deleted)
        {
            var removed = await MarkBranchRemoved(repo!, deleted, cancellationToken);

            return removed;
        }

        var candidates = BuildCandidates(scmEvent, repo!);
        var outcome = await Link(candidates, repo!, connection, cancellationToken);
        var isLiveEvent = source == ScmEventSource.Webhook;

        if (isLiveEvent)
        {
            await RecordActivity(scmEvent, repo!, connection, outcome, cancellationToken);
        }

        return outcome.Changed;
    }

    private async Task RecordActivity(
        ScmEvent scmEvent,
        SourceControlRepo repo,
        SourceControlConnection connection,
        LinkOutcome outcome,
        CancellationToken cancellationToken)
    {
        switch (scmEvent)
        {
            case ScmPullRequestChanged changed:
            {
                await RecordPullRequest(changed, repo, connection, outcome, cancellationToken);

                return;
            }

            case ScmCommitsPushed pushed:
            {
                await RecordPush(pushed, repo, connection, outcome, cancellationToken);

                return;
            }
        }
    }

    private async Task RecordPullRequest(
        ScmPullRequestChanged changed,
        SourceControlRepo repo,
        SourceControlConnection connection,
        LinkOutcome outcome,
        CancellationToken cancellationToken)
    {
        var eventKey = changed.Action switch
        {
            ScmPullRequestAction.Opened or ScmPullRequestAction.Reopened => EventKeys.SourceControlPullRequestOpened,
            ScmPullRequestAction.Merged => EventKeys.SourceControlPullRequestMerged,
            ScmPullRequestAction.Closed => EventKeys.SourceControlPullRequestClosed,
            _ => null,
        };

        var pullRequest = changed.PullRequest;
        var taskIds = outcome.TaskIdsFor(pullRequest.Number);

        AutomationTriggerType? triggerType = changed.Action switch
        {
            ScmPullRequestAction.Opened
                or ScmPullRequestAction.Reopened
                or ScmPullRequestAction.ReadyForReview => AutomationTriggerType.PullRequestOpened,
            ScmPullRequestAction.Merged => AutomationTriggerType.PullRequestMerged,
            _ => null,
        };

        var hasNothingToRecord = eventKey is null && triggerType is null;

        if (taskIds.Count == 0 || hasNothingToRecord)
        {
            return;
        }

        var isOpening = triggerType == AutomationTriggerType.PullRequestOpened;
        var actor = isOpening ? pullRequest.Author : changed.Actor ?? pullRequest.Author;
        var actorUserId = await Activity.ResolveActor(repo.WorkspaceId, connection.Provider, actor, cancellationToken);

        if (triggerType is not null)
        {
            QueueAutomation(changed, repo, triggerType.Value, taskIds, actorUserId);
        }

        if (eventKey is null)
        {
            return;
        }

        await Activity.RecordAs(new ScmTaskActivity
        {
            EventKey = eventKey,
            TaskIds = taskIds,
            Repo = repo,
            Provider = connection.Provider,
            Reference = pullRequest.Number,
            Title = Truncate(pullRequest.Title),
            WebUrl = pullRequest.WebUrl,
            Actor = actor,
            OccurredAt = changed.OccurredAt,
        }, actorUserId, cancellationToken);
    }

    private void QueueAutomation(
        ScmPullRequestChanged changed,
        SourceControlRepo repo,
        AutomationTriggerType triggerType,
        List<int> taskIds,
        string? actorUserId)
    {
        Automation.Add(new SourceControlTaskEventMessage
        {
            EventId = changed.EventId,
            WorkspaceId = repo.WorkspaceId,
            TriggerType = triggerType,
            TaskIds = taskIds,
            TargetBranch = changed.PullRequest.TargetBranch,
            IsDraft = changed.PullRequest.State == ScmPullRequestState.Draft,
            IsReadyForReview = changed.Action == ScmPullRequestAction.ReadyForReview,
            ActorUserId = actorUserId,
            OccurredAt = changed.OccurredAt.UtcDateTime,
        });
    }

    // Only commits linked for the first time count, so pushing the same commits again (a merge to the
    // default branch, say) doesn't repeat the entry.
    private async Task RecordPush(
        ScmCommitsPushed pushed,
        SourceControlRepo repo,
        SourceControlConnection connection,
        LinkOutcome outcome,
        CancellationToken cancellationToken)
    {
        var newCommitLinks = outcome.NewLinks
            .Where(link => link.Kind == TaskCodeLinkKind.Commit)
            .ToList();

        var pushesByTask = newCommitLinks
            .GroupBy(link => link.TaskId)
            .Select(group => new
            {
                TaskId = group.Key,
                CommitCount = group.Count(),
                Latest = group.MaxBy(link => link.OccurredAt)!,
            })
            .GroupBy(push => (push.CommitCount, push.Latest.ExternalRef))
            .ToList();

        foreach (var push in pushesByTask)
        {
            var latest = push.First().Latest;

            await Activity.Record(new ScmTaskActivity
            {
                EventKey = EventKeys.SourceControlCommitsPushed,
                TaskIds = push.Select(task => task.TaskId).ToList(),
                Repo = repo,
                Provider = connection.Provider,
                Reference = pushed.Branch,
                Title = latest.Title,
                WebUrl = latest.WebUrl,
                Actor = pushed.Actor ?? pushed.Commits.FirstOrDefault()?.Author,
                OccurredAt = pushed.OccurredAt,
                CommitCount = push.Key.CommitCount,
            }, cancellationToken);
        }
    }

    private async Task<LinkOutcome> Link(
        IReadOnlyList<LinkCandidate> candidates,
        SourceControlRepo repo,
        SourceControlConnection connection,
        CancellationToken cancellationToken)
    {
        var projectKeys = await UnitOfWork.Projects.GetProjectKeys(connection.WorkspaceId, cancellationToken);
        var extractor = new TaskReferenceExtractor(projectKeys);

        var referencesByCandidate = candidates.ToDictionary(
            candidate => candidate,
            candidate => extractor.Extract(candidate.Texts));

        var systemIds = referencesByCandidate.Values.SelectMany(references => references).Distinct().ToList();

        var taskIds = await ResolveTaskIds(systemIds, connection, cancellationToken);

        var now = Clock.GetUtcNow().UtcDateTime;
        var changed = false;
        var newLinks = new List<TaskCodeLink>();
        var linkedTaskIdsByRef = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        foreach (var group in candidates.GroupBy(candidate => candidate.Kind))
        {
            var externalRefs = group.Select(candidate => candidate.ExternalRef).Distinct().ToList();
            var existing = await UnitOfWork.TaskCodeLinks.GetForRefs(
                repo.Id,
                group.Key,
                externalRefs,
                cancellationToken);

            foreach (var candidate in group)
            {
                var existingForRef = existing.Where(link => link.ExternalRef == candidate.ExternalRef).ToList();

                // Updates reach every task already linked, even when this payload no longer mentions
                // the task: a pull request whose title was edited is still the same pull request.
                // A delivery older than what's stored is a retry or arrived out of order, and is skipped.
                var staleLinks = existingForRef.Where(link => IsOlderThan(candidate, link)).ToList();

                foreach (var link in existingForRef.Except(staleLinks))
                {
                    Apply(link, candidate, now);
                    changed = true;
                }

                var linkedTaskIds = existingForRef.Select(link => link.TaskId).ToHashSet();

                var newTaskIds = referencesByCandidate[candidate]
                    .Where(taskIds.ContainsKey)
                    .Select(systemId => taskIds[systemId])
                    .Where(taskId => !linkedTaskIds.Contains(taskId))
                    .Distinct()
                    .ToList();

                newLinks.AddRange(newTaskIds.Select(taskId => Create(candidate, repo, taskId, now)));

                var refTaskIds = linkedTaskIdsByRef.GetValueOrDefault(candidate.ExternalRef) ?? [];

                refTaskIds.UnionWith(linkedTaskIds);
                refTaskIds.UnionWith(newTaskIds);
                linkedTaskIdsByRef[candidate.ExternalRef] = refTaskIds;
            }
        }

        if (newLinks.Count > 0)
        {
            await UnitOfWork.TaskCodeLinks.AddRangeAsync(newLinks, cancellationToken);
            changed = true;
        }

        return new LinkOutcome(changed, newLinks, linkedTaskIdsByRef);
    }

    private async Task<Dictionary<string, int>> ResolveTaskIds(
        List<string> systemIds,
        SourceControlConnection connection,
        CancellationToken cancellationToken)
    {
        if (systemIds.Count == 0)
        {
            return [];
        }

        var taskIds = await UnitOfWork.Tasks.GetTaskIdsBySystemIds(
            systemIds,
            connection.Workspace!.Slug,
            includeArchived: false,
            cancellationToken);

        return taskIds;
    }

    private async Task<bool> MarkBranchRemoved(
        SourceControlRepo repo,
        ScmBranchDeleted deleted,
        CancellationToken cancellationToken)
    {
        var links = await UnitOfWork.TaskCodeLinks.GetForRefs(
            repo.Id,
            TaskCodeLinkKind.Branch,
            [deleted.Branch],
            cancellationToken);

        var deletedAt = deleted.OccurredAt.UtcDateTime;
        var linksToRemove = links.Where(link => link.OccurredAt <= deletedAt).ToList();

        if (linksToRemove.Count == 0)
        {
            return false;
        }

        var now = Clock.GetUtcNow().UtcDateTime;

        foreach (var link in linksToRemove)
        {
            link.IsRemoved = true;
            link.OccurredAt = deletedAt;
            link.UpdatedAt = now;
        }

        return true;
    }

    private static List<LinkCandidate> BuildCandidates(ScmEvent scmEvent, SourceControlRepo repo)
    {
        List<LinkCandidate> candidates = scmEvent switch
        {
            ScmBranchCreated created => [FromBranch(created)],
            ScmCommitsPushed pushed => pushed.Commits.Select(commit => FromCommit(commit, pushed, repo)).ToList(),
            ScmPullRequestChanged changed => [FromPullRequest(changed.PullRequest)],
            _ => [],
        };

        var storable = candidates.Where(FitsStorage).ToList();

        return storable;
    }

    // Git allows branch names longer than the column; those are left unlinked rather than truncated,
    // because two truncated names could collide on the same row.
    private static bool FitsStorage(LinkCandidate candidate)
    {
        var refFits = candidate.ExternalRef.Length <= MaxExternalRefLength;
        var urlFits = candidate.WebUrl.Length <= MaxWebUrlLength;

        return refFits && urlFits;
    }

    private static bool IsOlderThan(LinkCandidate candidate, TaskCodeLink link)
    {
        return candidate.OccurredAt.UtcDateTime < link.OccurredAt;
    }

    private static LinkCandidate FromBranch(ScmBranchCreated created)
    {
        return new LinkCandidate
        {
            Kind = TaskCodeLinkKind.Branch,
            ExternalRef = created.Branch,
            Title = created.Branch,
            WebUrl = created.WebUrl,
            AuthorLogin = Bounded(created.Actor?.Username),
            AuthorExternalId = created.Actor?.ExternalId,
            OccurredAt = created.OccurredAt,
            Texts = [created.Branch],
        };
    }

    // Commits pick up the branch's references too, so work on `feature/netp-564-x` links even when
    // the messages don't repeat the id. The default branch is excluded, or a merge would link everything.
    private static LinkCandidate FromCommit(ScmCommit commit, ScmCommitsPushed pushed, SourceControlRepo repo)
    {
        var isDefaultBranch = string.Equals(pushed.Branch, repo.DefaultBranch, StringComparison.Ordinal);
        var branchText = isDefaultBranch ? null : pushed.Branch;
        var subject = commit.Message.Split('\n', 2)[0].Trim();

        return new LinkCandidate
        {
            Kind = TaskCodeLinkKind.Commit,
            ExternalRef = commit.Sha,
            Title = subject,
            WebUrl = commit.WebUrl,
            AuthorLogin = Bounded(commit.Author.Username),
            AuthorExternalId = commit.Author.ExternalId,
            AuthorEmail = Bounded(commit.Author.Email),
            OccurredAt = commit.AuthoredAt,
            Texts = [commit.Message, branchText],
        };
    }

    private static LinkCandidate FromPullRequest(ScmPullRequest pullRequest)
    {
        return new LinkCandidate
        {
            Kind = TaskCodeLinkKind.PullRequest,
            ExternalRef = pullRequest.Number,
            Title = pullRequest.Title,
            WebUrl = pullRequest.WebUrl,
            AuthorLogin = Bounded(pullRequest.Author.Username),
            AuthorExternalId = pullRequest.Author.ExternalId,
            OccurredAt = pullRequest.UpdatedAt,
            PullRequestState = pullRequest.State,
            HeadSha = pullRequest.HeadSha,
            Texts = [pullRequest.Title, pullRequest.Body, pullRequest.SourceBranch],
        };
    }

    private static TaskCodeLink Create(LinkCandidate candidate, SourceControlRepo repo, int taskId, DateTime now)
    {
        return new TaskCodeLink
        {
            WorkspaceId = repo.WorkspaceId,
            TaskId = taskId,
            RepoId = repo.Id,
            Kind = candidate.Kind,
            ExternalRef = candidate.ExternalRef,
            Title = Truncate(candidate.Title),
            WebUrl = candidate.WebUrl,
            AuthorLogin = candidate.AuthorLogin,
            AuthorExternalId = candidate.AuthorExternalId,
            AuthorEmail = candidate.AuthorEmail,
            PullRequestState = candidate.PullRequestState,
            HeadSha = candidate.HeadSha,
            OccurredAt = candidate.OccurredAt.UtcDateTime,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static void Apply(TaskCodeLink link, LinkCandidate candidate, DateTime now)
    {
        link.Title = Truncate(candidate.Title);
        link.WebUrl = candidate.WebUrl;
        link.AuthorLogin = candidate.AuthorLogin ?? link.AuthorLogin;
        link.AuthorExternalId = candidate.AuthorExternalId ?? link.AuthorExternalId;
        link.AuthorEmail = candidate.AuthorEmail ?? link.AuthorEmail;
        link.PullRequestState = candidate.PullRequestState ?? link.PullRequestState;
        var headMoved = candidate.HeadSha is not null && candidate.HeadSha != link.HeadSha;

        link.HeadSha = candidate.HeadSha ?? link.HeadSha;
        link.LastCheckState = headMoved ? null : link.LastCheckState;
        link.OccurredAt = candidate.OccurredAt.UtcDateTime;
        link.IsRemoved = false;
        link.UpdatedAt = now;
    }

    // Git puts no limit on author names or emails, and an author is optional on a link.
    private static string? Bounded(string? value)
    {
        var fits = value is not null && value.Length <= MaxAuthorLength;

        return fits ? value : null;
    }

    private static string Truncate(string value)
    {
        return value.Length > MaxTitleLength ? value[..MaxTitleLength] : value;
    }

    private static ScmRepositoryRef RepositoryOf(ScmEvent scmEvent)
    {
        return scmEvent switch
        {
            ScmBranchCreated created => created.Repository,
            ScmBranchDeleted deleted => deleted.Repository,
            ScmCommitsPushed pushed => pushed.Repository,
            ScmPullRequestChanged changed => changed.Repository,
            _ => throw new ArgumentOutOfRangeException(nameof(scmEvent), scmEvent.GetType().Name, null),
        };
    }

    private sealed record LinkCandidate
    {
        public TaskCodeLinkKind Kind { get; init; }

        public required string ExternalRef { get; init; }

        public required string Title { get; init; }

        public required string WebUrl { get; init; }

        public string? AuthorLogin { get; init; }

        public string? AuthorExternalId { get; init; }

        public string? AuthorEmail { get; init; }

        public DateTimeOffset OccurredAt { get; init; }

        public ScmPullRequestState? PullRequestState { get; init; }

        public string? HeadSha { get; init; }

        public required string?[] Texts { get; init; }
    }

    private sealed record LinkOutcome(
        bool Changed,
        List<TaskCodeLink> NewLinks,
        Dictionary<string, HashSet<int>> TaskIdsByRef)
    {
        public List<int> TaskIdsFor(string externalRef)
        {
            var taskIds = TaskIdsByRef.GetValueOrDefault(externalRef);

            return taskIds is null ? [] : taskIds.ToList();
        }
    }
}
