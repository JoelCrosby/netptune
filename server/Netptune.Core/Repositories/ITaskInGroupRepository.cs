using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ITaskInGroupRepository : IRepository<ProjectTaskInBoardGroup, int>
{
    Task<ProjectTaskInBoardGroup?> GetProjectTaskInGroup(int taskId, int groupId, CancellationToken cancellationToken = default);

    Task<ProjectTaskInBoardGroup?> GetPlacementOnBoard(int taskId, int boardId, CancellationToken cancellationToken = default);

    Task<Dictionary<int, int>> GetPlacementGroupsOnBoard(IReadOnlyCollection<int> taskIds, int boardId, CancellationToken cancellationToken = default);

    Task DeleteAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default);

    Task DeleteTasksFromBoard(IEnumerable<int> taskIds, int boardId, CancellationToken cancellationToken = default);

    Task MovePlacementsToGroup(int fromGroupId, int toGroupId, double baseSortOrder, CancellationToken cancellationToken = default);

    Task DeletePlacementsInGroup(int groupId, CancellationToken cancellationToken = default);

    Task<(double? Previous, double? Next)> GetNeighborSortOrdersForInsert(int groupId, int taskId, int currentIndex, CancellationToken cancellationToken = default);
}
