using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ITaskViewRepository : IWorkspaceEntityRepository<TaskView, int>
{
    Task<List<TaskView>> GetVisibleInWorkspace(int workspaceId, string currentUserId, CancellationToken cancellationToken = default);

    Task<bool> NameExists(int workspaceId, string name, int? excludeId, CancellationToken cancellationToken = default);

    Task<TaskView?> GetBySlug(string slug, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);
}
