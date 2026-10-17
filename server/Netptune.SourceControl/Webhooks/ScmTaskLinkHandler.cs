using Netptune.Core.Entities;
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
    private readonly TimeProvider Clock;

    public ScmTaskLinkHandler(INetptuneUnitOfWork unitOfWork, TimeProvider clock)
    {
        UnitOfWork = unitOfWork;
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
        var linked = await Link(candidates, repo!, connection, cancellationToken);

        return linked;
    }

    private async Task<bool> Link(
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
            }
        }

        if (newLinks.Count > 0)
        {
            await UnitOfWork.TaskCodeLinks.AddRangeAsync(newLinks, cancellationToken);
            changed = true;
        }

        if (changed)        {
            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        return changed;
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

        await UnitOfWork.CompleteAsync(cancellationToken);

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
        link.HeadSha = candidate.HeadSha ?? link.HeadSha;
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
}
