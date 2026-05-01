using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IWorkspaceRepository : IRepository<Workspace, int>
{
    Task<int?> GetIdBySlug(string slug, CancellationToken cancellationToken = default);

    Task<Workspace?> GetBySlug(string slug, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<Workspace?> GetBySlugWithTasks(string slug, bool includeRelated, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<Workspace>> GetUserWorkspaces(string userId, CancellationToken cancellationToken = default);

    Task<bool> Exists(string slug, CancellationToken cancellationToken = default);

    Task<Dictionary<int, string>> GetSlugsByIds(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
