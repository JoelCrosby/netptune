using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Core.Repositories;

public interface ISprintRepository : IWorkspaceEntityRepository<Sprint, int>
{
    Task<List<SprintViewModel>> GetSprintsAsync(
        string workspaceKey,
        int? projectId = null,
        SprintStatus? status = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    Task<SprintDetailViewModel?> GetSprintDetailAsync(
        string workspaceKey,
        int sprintId,
        CancellationToken cancellationToken = default);

    Task<Sprint?> GetSprintInWorkspaceAsync(
        string workspaceKey,
        int sprintId,
        bool isReadonly = false,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveSprintAsync(
        int projectId,
        int? excludingSprintId = null,
        CancellationToken cancellationToken = default);
}
