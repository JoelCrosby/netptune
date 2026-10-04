using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Repositories;

public interface IBoardRepository : IWorkspaceEntityRepository<Board, int>
{
    Task<List<Board>> GetBoardsInProject(int projectId, bool isReadonly = false, bool includeGroups = false, CancellationToken cancellationToken = default, PageRequest? pageRequest = null);

    Task<List<Board>> GetBoards(string slug, bool isReadonly = false, CancellationToken cancellationToken = default);

    // Boards with the given ids in a single query, with their project loaded.
    Task<List<Board>> GetBoardsById(IEnumerable<int> boardIds, CancellationToken cancellationToken = default);

    // One page of the workspace's boards, ordered by project, filtered by board name, identifier or project name.
    Task<PagedResponse<BoardViewModel>> GetBoardsPage(string slug, BoardFilter filter, CancellationToken cancellationToken = default);

    // A single board with the same project, task count and assignee detail as GetBoardsPage.
    Task<BoardViewModel?> GetWorkspaceBoardViewModel(string slug, int boardId, CancellationToken cancellationToken = default);

    Task<Board?> GetByIdentifier(string identifier, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<int?> GetIdByIdentifier(string identifier, int workspaceId, CancellationToken cancellationToken = default);

    Task<BoardViewModel?> GetViewModel(int id, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<bool> Exists(string identifier, CancellationToken cancellationToken = default);

    Task SetBrandingFile(int boardId, int workspaceId, string metaKey, string? fileId, CancellationToken cancellationToken = default);
}
