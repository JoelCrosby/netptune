using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ITaskPinRepository : IWorkspaceEntityRepository<TaskPin, int>
{
    Task<List<TaskPin>> GetVisibleInWorkspace(int workspaceId, string userId, CancellationToken cancellationToken = default);

    Task<List<TaskPin>> GetForBoard(int boardId, int projectId, int workspaceId, string userId, CancellationToken cancellationToken = default);

    Task<List<TaskPin>> GetForScopeEntity(int workspaceId, TaskPinScope scope, int scopeEntityId, CancellationToken cancellationToken = default);

    Task<TaskPin?> Find(int taskId, TaskPinScope scope, int scopeEntityId, string userId, CancellationToken cancellationToken = default);

    Task<List<TaskPin>> GetByIds(IReadOnlyCollection<int> ids, int workspaceId, CancellationToken cancellationToken = default);

    Task<double> GetNextSortOrder(int workspaceId, TaskPinScope scope, int scopeEntityId, CancellationToken cancellationToken = default);
}
