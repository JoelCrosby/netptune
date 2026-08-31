using Netptune.Core.Entities;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Repositories;

public interface IBoardGroupRepository : IWorkspaceEntityRepository<BoardGroup, int>
{

    Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<BoardViewGroup>?> GetBoardViewGroups(int boardId, string currentUserId, string? searchTerm = null, int? sprintId = null, CancellationToken cancellationToken = default);

    Task<BoardGroupTaskTarget?> GetTaskTarget(int groupId, CancellationToken cancellationToken = default);

    Task<BoardGroupTaskTarget?> GetDefaultTaskTarget(int projectId, CancellationToken cancellationToken = default);

    Task<BoardGroupTaskTarget?> GetStatusTaskTarget(int boardId, int statusId, CancellationToken cancellationToken = default);

    Task<BoardGroupTaskTarget?> GetFallbackTaskTarget(int boardId, int? excludeGroupId = null, CancellationToken cancellationToken = default);

    Task<List<BoardGroupOptionViewModel>> GetOptionsInWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<double> GetMaxTaskSortOrder(int groupId, CancellationToken cancellationToken = default);

    ValueTask<double> GetBoardGroupDefaultSortOrder(int boardId, CancellationToken cancellationToken = default);
}
