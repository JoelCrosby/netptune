using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Repositories;

public interface IBoardGroupRepository : IWorkspaceEntityRepository<BoardGroup, int>
{
    Task<BoardGroup?> GetWithTasksInGroups(int id, CancellationToken cancellationToken = default);

    Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<BoardViewGroup>?> GetBoardViewGroups(
        int boardId,
        string? searchTerm = null,
        int? sprintId = null,
        CancellationToken cancellationToken = default);

    Task<List<BoardViewGroup>?> GetBoardViewGroups(
        int boardId,
        string? searchTerm,
        CancellationToken cancellationToken);

    Task<List<BoardGroup>> GetBoardGroupsForProjectTask(int taskId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<ProjectTask>> GetTasksInGroup(int groupId, bool isReadonly = false, CancellationToken cancellationToken = default);

    ValueTask<double> GetBoardGroupDefaultSortOrder(int boardId, CancellationToken cancellationToken = default);

    Task<int?> GetBoardGroupIdForTask(int projectTaskId, CancellationToken cancellationToken = default);
}
