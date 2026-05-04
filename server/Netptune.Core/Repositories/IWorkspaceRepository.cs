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

    Task<Dictionary<int, string>> GetSlugsByIds(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
