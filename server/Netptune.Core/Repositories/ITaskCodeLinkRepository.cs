using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.History;

namespace Netptune.Core.Repositories;

public interface ITaskCodeLinkRepository : IRepository<TaskCodeLink, long>
{
    Task<List<TaskCodeLink>> GetForRefs(int repoId, TaskCodeLinkKind kind, IReadOnlyCollection<string> externalRefs, CancellationToken cancellationToken = default);

    Task<List<TaskCodeLink>> GetForTask(int taskId, int workspaceId, CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetRepoIdsWithLinks(IReadOnlyCollection<int> repoIds, CancellationToken cancellationToken = default);

    Task<List<OpenPullRequestHead>> GetOpenPullRequestHeads(int repoId, CancellationToken cancellationToken = default);

    Task<List<TaskCodeLink>> GetReferencingCommit(int repoId, string commitSha, CancellationToken cancellationToken = default);
}
