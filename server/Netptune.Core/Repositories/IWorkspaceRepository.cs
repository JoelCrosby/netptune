using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;

namespace Netptune.Core.Repositories;

public interface IWorkspaceRepository : IRepository<Workspace, int>
{
    Task<int?> GetIdBySlug(string slug, CancellationToken cancellationToken = default);

    Task<Workspace?> GetBySlug(string slug, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<Workspace?> GetBySlugWithTasks(string slug, bool includeRelated, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<Workspace>> GetUserWorkspaces(string userId, CancellationToken cancellationToken = default, PageRequest? pageRequest = null);

    Task<List<Workspace>> GetWorkspaces(CancellationToken cancellationToken = default, PageRequest? pageRequest = null);

    Task<bool> Exists(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes a workspace and every row that hangs off it.
    /// <para>
    /// Every foreign key into a workspace-scoped table is <c>Restrict</c>, so the workspace row can
    /// only go once nothing points at it any more. The delete order is the whole substance of this
    /// method — it lives in one place so a new workspace-scoped table has one obvious spot to be
    /// added to.
    /// </para>
    /// </summary>
    Task DeleteWorkspacePermanent(int workspaceId, CancellationToken cancellationToken = default);

    Task<Dictionary<int, string>> GetSlugsByIds(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
