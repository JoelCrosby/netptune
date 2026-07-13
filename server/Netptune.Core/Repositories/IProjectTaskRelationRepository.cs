using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Relations;

namespace Netptune.Core.Repositories;

public interface IProjectTaskRelationRepository : IRepository<ProjectTaskRelation, int>
{
    Task<List<TaskRelationViewModel>> GetRelationsForTask(int taskId, int workspaceId, CancellationToken cancellationToken = default);

    Task<ProjectTaskRelation?> GetInWorkspace(int id, int workspaceId, CancellationToken cancellationToken = default);

    Task<bool> Exists(int relationTypeId, int sourceTaskId, int targetTaskId, CancellationToken cancellationToken = default);

    Task<bool> HasExistingSource(int relationTypeId, int targetTaskId, CancellationToken cancellationToken = default);

    Task<bool> WouldCreateCycle(int relationTypeId, int sourceTaskId, int targetTaskId, CancellationToken cancellationToken = default);

    Task<List<int>> DeleteAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default);
}
