using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Files;

namespace Netptune.Core.Repositories;

public interface IWorkspaceRepository : IRepository<Workspace, int>
{
    Task<int?> GetIdBySlug(string slug, CancellationToken cancellationToken = default);

    Task<Workspace?> GetBySlug(string slug, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<Workspace?> GetBySlugWithTasks(string slug, bool includeRelated, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<Workspace>> GetUserWorkspaces(string userId, CancellationToken cancellationToken = default, PageRequest? pageRequest = null);

    Task<List<Workspace>> GetWorkspaces(CancellationToken cancellationToken = default, PageRequest? pageRequest = null);

    Task<bool> Exists(string slug, CancellationToken cancellationToken = default);

    Task DeleteWorkspacePermanent(int workspaceId, CancellationToken cancellationToken = default);

    Task<Dictionary<int, string>> GetSlugsByIds(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    Task<WorkspaceStorageUsageViewModel?> GetStorageUsage(int workspaceId, CancellationToken cancellationToken = default);

    Task<bool> TryReserveStorage(int workspaceId, long sizeBytes, CancellationToken cancellationToken = default);

    Task ReleaseStorage(int workspaceId, long sizeBytes, CancellationToken cancellationToken = default);

    Task<List<int>> GetAllIds(CancellationToken cancellationToken = default);

    Task<Workspace?> GetForStorageUpdate(int id, CancellationToken cancellationToken = default);

    Task SetStorageUsage(int id, long sizeBytes, CancellationToken cancellationToken = default);
}
