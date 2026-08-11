using Netptune.Core.Models.Automations;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Relations;

namespace Netptune.Core.Repositories;

public interface IProjectTaskRelationRepository : IRepository<ProjectTaskRelation, int>
{
    Task<List<TaskRelationViewModel>> GetRelationsForTask(int taskId, int workspaceId, CancellationToken cancellationToken = default);

    Task<PagedResponse<RelationTypeRelationViewModel>> GetRelationsForType(int relationTypeId, int workspaceId, PageRequest pageRequest, CancellationToken cancellationToken = default);

    Task<ProjectTaskRelation?> GetInWorkspace(int id, int workspaceId, CancellationToken cancellationToken = default);

    Task<bool> Exists(int relationTypeId, int sourceTaskId, int targetTaskId, CancellationToken cancellationToken = default);

    Task<bool> HasExistingSource(int relationTypeId, int targetTaskId, CancellationToken cancellationToken = default);

    Task<List<int>> GetTargetsWithExistingSource(int relationTypeId, IReadOnlyCollection<int> targetTaskIds, CancellationToken cancellationToken = default);

    Task<bool> WouldCreateCycle(int relationTypeId, int sourceTaskId, int targetTaskId, CancellationToken cancellationToken = default);

    Task<List<int>> GetReachableTaskIds(int relationTypeId, IReadOnlyCollection<int> fromTaskIds, CancellationToken cancellationToken = default);

    Task<List<ProjectTaskRelation>> GetForTaskAndType(int relationTypeId, int taskId, int? relatedTaskId, CancellationToken cancellationToken = default);

    Task<List<int>> DeleteAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default);

    Task<List<TaskRelationCounts>> GetBlockerCounts(IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default);

    Task<List<TaskRelationCounts>> GetChildCounts(IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default);

    Task<List<int>> GetDependentTaskIds(IReadOnlyCollection<int> blockingTaskIds, CancellationToken cancellationToken = default);

    Task<List<int>> GetParentTaskIds(IReadOnlyCollection<int> childTaskIds, CancellationToken cancellationToken = default);
}
