using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Statuses;

namespace Netptune.Core.Repositories;

public interface IStatusRepository : IWorkspaceEntityRepository<Status, int>
{
    Task<List<StatusViewModel>> GetViewModelsForWorkspace(int workspaceId, EntityType entityType, CancellationToken cancellationToken = default);

    Task<StatusViewModel?> GetViewModel(int id, CancellationToken cancellationToken = default);

    Task<Status?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<Status?> GetTaskStatusByKey(int workspaceId, string key, CancellationToken cancellationToken = default);

    Task<Status?> GetFirstTaskStatus(int workspaceId, CancellationToken cancellationToken = default);

    Task<Status?> GetFirstTaskStatusByCategory(int workspaceId, StatusCategory category, CancellationToken cancellationToken = default);

    Task<bool> KeyExists(int workspaceId, EntityType entityType, string key, int? excludingId = null, CancellationToken cancellationToken = default);

    Task<bool> IsInUse(int statusId, CancellationToken cancellationToken = default);

    Task EnsureDefaultTaskStatuses(int workspaceId, string? ownerId, CancellationToken cancellationToken = default);

    Task EnsureNewTaskStatus(int workspaceId, string? ownerId, CancellationToken cancellationToken = default);
}
