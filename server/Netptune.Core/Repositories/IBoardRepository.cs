using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Repositories;

public interface IBoardRepository : IWorkspaceEntityRepository<Board, int>
{
    Task<List<Board>> GetBoardsInProject(int projectId, bool isReadonly = false, bool includeGroups = false);

    Task<Board> GetDefaultBoardInProject(int projectId, bool isReadonly = false, bool includeGroups = false);

    Task<List<Board>> GetBoards(string slug, bool isReadonly = false);

    Task<List<BoardsViewModel>> GetBoardViewModels(string slug);

    Task<Board> GetByIdentifier(string identifier, int workspaceId, bool isReadonly = false);

    Task<int?> GetIdByIdentifier(string identifier, int workspaceId);

    Task<BoardViewModel> GetViewModel(int id, bool isReadonly = false);

    Task<bool> Exists(string identifier);
}