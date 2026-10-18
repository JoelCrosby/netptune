using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.History;
using Netptune.Core.SourceControl.Models;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class TaskCodeLinkRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, TaskCodeLink, long>(context, connectionFactory), ITaskCodeLinkRepository
{
    public Task<List<TaskCodeLink>> GetForRefs(
        int repoId,
        TaskCodeLinkKind kind,
        IReadOnlyCollection<string> externalRefs,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(link => link.RepoId == repoId && link.Kind == kind)
            .Where(link => externalRefs.Contains(link.ExternalRef))
            .ToListAsync(cancellationToken);
    }

    public Task<List<TaskCodeLink>> GetForTask(
        int taskId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Include(link => link.Repo)
            .ThenInclude(repo => repo.Connection)
            .Where(link => link.TaskId == taskId && link.WorkspaceId == workspaceId)
            .Where(link => link.Repo.IsLinked && !link.Repo.Connection.IsDeleted)
            .OrderByDescending(link => link.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<int>> GetRepoIdsWithLinks(
        IReadOnlyCollection<int> repoIds,
        CancellationToken cancellationToken = default)
    {
        var linkedRepoIds = await Entities
            .Where(link => repoIds.Contains(link.RepoId))
            .Select(link => link.RepoId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return linkedRepoIds.ToHashSet();
    }

    public Task<List<TaskCodeLink>> GetReferencingCommit(
        int repoId,
        string commitSha,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(link => link.RepoId == repoId && !link.IsRemoved)
            .Where(link => (link.Kind == TaskCodeLinkKind.Commit && link.ExternalRef == commitSha)
                || (link.Kind == TaskCodeLinkKind.PullRequest && link.HeadSha == commitSha))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OpenPullRequestHead>> GetOpenPullRequestHeads(
        int repoId,
        CancellationToken cancellationToken = default)
    {
        var heads = await Entities
            .AsNoTracking()
            .Where(link => link.RepoId == repoId && !link.IsRemoved && link.Kind == TaskCodeLinkKind.PullRequest)
            .Where(link => link.PullRequestState == ScmPullRequestState.Open
                || link.PullRequestState == ScmPullRequestState.Draft)
            .Where(link => link.HeadSha != null)
            .Select(link => new { link.ExternalRef, link.HeadSha })
            .Distinct()
            .ToListAsync(cancellationToken);

        return heads.Select(head => new OpenPullRequestHead(head.ExternalRef, head.HeadSha!)).ToList();
    }
}
