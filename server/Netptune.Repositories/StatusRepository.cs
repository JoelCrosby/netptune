using Microsoft.EntityFrameworkCore;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Statuses;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class StatusRepository : WorkspaceEntityRepository<DataContext, Status, int>, IStatusRepository
{
    public StatusRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<StatusViewModel>> GetViewModelsForWorkspace(int workspaceId, EntityType entityType, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(status => status.WorkspaceId == workspaceId && status.EntityType == entityType && !status.IsDeleted)
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Id)
            .AsNoTracking()
            .Select(status => new StatusViewModel
            {
                Id = status.Id,
                WorkspaceId = status.WorkspaceId,
                EntityType = status.EntityType,
                Name = status.Name,
                Key = status.Key,
                Description = status.Description,
                Color = status.Color,
                SortOrder = status.SortOrder,
                Category = status.Category,
                IsSystem = status.IsSystem,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<StatusViewModel?> GetViewModel(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(status => status.Id == id && !status.IsDeleted)
            .AsNoTracking()
            .Select(status => new StatusViewModel
            {
                Id = status.Id,
                WorkspaceId = status.WorkspaceId,
                EntityType = status.EntityType,
                Name = status.Name,
                Key = status.Key,
                Description = status.Description,
                Color = status.Color,
                SortOrder = status.SortOrder,
                Category = status.Category,
                IsSystem = status.IsSystem,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Status?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(status => status.Id == id && status.WorkspaceId == workspaceId && !status.IsDeleted)
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Status?> GetTaskStatusByKey(int workspaceId, string key, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(status =>
                status.WorkspaceId == workspaceId &&
                status.EntityType == EntityType.Task &&
                status.Key == key &&
                !status.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Status?> GetFirstTaskStatus(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(status => status.WorkspaceId == workspaceId && status.EntityType == EntityType.Task && !status.IsDeleted)
            .OrderByDescending(status => status.Key == "new")
            .ThenBy(status => status.SortOrder)
            .ThenBy(status => status.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Status?> GetFirstTaskStatusByCategory(int workspaceId, StatusCategory category, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(status =>
                status.WorkspaceId == workspaceId &&
                status.EntityType == EntityType.Task &&
                status.Category == category &&
                !status.IsDeleted)
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> KeyExists(int workspaceId, EntityType entityType, string key, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(status =>
            status.WorkspaceId == workspaceId &&
            status.EntityType == entityType &&
            status.Key == key &&
            !status.IsDeleted &&
            (!excludingId.HasValue || status.Id != excludingId.Value), cancellationToken);
    }

    public async Task<bool> IsInUse(int statusId, CancellationToken cancellationToken = default)
    {
        var taskUsesStatus = await Context.ProjectTasks.AnyAsync(task => task.StatusId == statusId && !task.IsDeleted, cancellationToken);
        if (taskUsesStatus) return true;

        return await Context.Projects.AnyAsync(project => project.DefaultStatusId == statusId && !project.IsDeleted, cancellationToken);
    }

    public async Task EnsureDefaultTaskStatuses(int workspaceId, string? ownerId, CancellationToken cancellationToken = default)
    {
        var existingKeys = await Entities
            .Where(status => status.WorkspaceId == workspaceId && status.EntityType == EntityType.Task)
            .Select(status => status.Key)
            .ToListAsync(cancellationToken);

        var existingSet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = DefaultTaskStatuses.All
            .Where(definition => !existingSet.Contains(definition.Key))
            .Select(definition => DefaultTaskStatuses.Create(definition, workspaceId, ownerId))
            .ToList();

        if (missing.Count == 0) return;

        await Entities.AddRangeAsync(missing, cancellationToken);
    }

    public static string BuildKey(string name)
    {
        var key = name.Trim().ToUrlSlug();
        return string.IsNullOrWhiteSpace(key) ? Guid.NewGuid().ToString("N")[..8] : key;
    }
}
