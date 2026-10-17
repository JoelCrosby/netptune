using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;
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

    public Task<bool> ReferencesCommit(int repoId, string commitSha, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(link => link.RepoId == repoId && !link.IsRemoved)
            .Where(link => (link.Kind == TaskCodeLinkKind.Commit && link.ExternalRef == commitSha)
                || (link.Kind == TaskCodeLinkKind.PullRequest && link.HeadSha == commitSha))
            .AnyAsync(cancellationToken);
    }
}
