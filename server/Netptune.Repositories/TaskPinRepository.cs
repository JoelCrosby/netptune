using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class TaskPinRepository : WorkspaceEntityRepository<DataContext, TaskPin, int>, ITaskPinRepository
{
    public TaskPinRepository(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory) { }

    public Task<List<TaskPin>> GetVisibleInWorkspace(int workspaceId, string? userId, CancellationToken cancellationToken = default)
    {
        var includePersonal = userId is not null;

        return LivePins(workspaceId)
            .Where(pin => pin.Scope != TaskPinScope.User || (includePersonal && pin.CreatedByUserId == userId))
            .OrderBy(pin => pin.SortOrder)
            .ThenByDescending(pin => pin.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<TaskPin>> GetForBoard(int boardId, int projectId, int workspaceId, string? userId, CancellationToken cancellationToken = default)
    {
        var includePersonal = userId is not null;

        return LivePins(workspaceId)
            .Where(pin =>
                (includePersonal && pin.Scope == TaskPinScope.User && pin.ScopeEntityId == workspaceId && pin.CreatedByUserId == userId) ||
                (pin.Scope == TaskPinScope.Board && pin.ScopeEntityId == boardId) ||
                (pin.Scope == TaskPinScope.Project && pin.ScopeEntityId == projectId) ||
                (pin.Scope == TaskPinScope.Workspace && pin.ScopeEntityId == workspaceId))
            .OrderBy(pin => pin.SortOrder)
            .ThenByDescending(pin => pin.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<TaskPin>> GetForScopeEntity(int workspaceId, TaskPinScope scope, int scopeEntityId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(pin => pin.WorkspaceId == workspaceId && !pin.IsDeleted)
            .Where(pin => pin.Scope == scope && pin.ScopeEntityId == scopeEntityId)
            .ToListAsync(cancellationToken);
    }

    public Task<TaskPin?> Find(int taskId, TaskPinScope scope, int scopeEntityId, string userId, CancellationToken cancellationToken = default)
    {
        var isPersonal = scope == TaskPinScope.User;

        return Entities
            .Where(pin => pin.ProjectTaskId == taskId && pin.Scope == scope && pin.ScopeEntityId == scopeEntityId)
            .Where(pin => !isPersonal || pin.CreatedByUserId == userId)
            .OrderBy(pin => pin.IsDeleted)
            .ThenByDescending(pin => pin.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<TaskPin>> GetByIds(IReadOnlyCollection<int> ids, int workspaceId, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Task.FromResult(new List<TaskPin>());
        }

        return Entities
            .Where(pin => ids.Contains(pin.Id) && pin.WorkspaceId == workspaceId && !pin.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    // Sort order ascends, so the newest pin takes the slot above everything already pinned at this
    // scope. Recency and manual order then read the same way round and one list can serve both.
    public async Task<double> GetNextSortOrder(int workspaceId, TaskPinScope scope, int scopeEntityId, CancellationToken cancellationToken = default)
    {
        var lowest = await Entities
            .AsNoTracking()
            .Where(pin => pin.WorkspaceId == workspaceId && !pin.IsDeleted)
            .Where(pin => pin.Scope == scope && pin.ScopeEntityId == scopeEntityId)
            .Select(pin => (double?)pin.SortOrder)
            .MinAsync(cancellationToken);

        if (lowest is null)
        {
            return 0d;
        }

        return lowest.Value - 1d;
    }

    // A pin whose task has been soft-deleted must not surface anywhere, and the cascade only fires
    // on a hard delete, so every read path filters through project_tasks.
    private IQueryable<TaskPin> LivePins(int workspaceId)
    {
        return Entities
            .AsNoTracking()
            .Where(pin => pin.WorkspaceId == workspaceId && !pin.IsDeleted)
            .Where(pin => Context.ProjectTasks.Any(task => task.Id == pin.ProjectTaskId && !task.IsDeleted));
    }
}
