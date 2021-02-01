using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Services
{
    public interface IBoardService
    {
        Task<List<BoardViewModel>> GetBoards(int projectId);

        Task<List<BoardViewModel>> GetBoardsInProject(int projectId);

        Task<BoardViewModel> GetBoard(int id);

        Task<BoardView> GetBoardView(string boardIdentifier, BoardGroupsFilter filter = null);

        Task<ClientResponse<BoardViewModel>> UpdateBoard(Board board);

        Task<ClientResponse<BoardViewModel>> AddBoard(AddBoardRequest board);

        Task<ClientResponse> Delete(int id);

        Task<List<BoardsViewModel>> GetBoardsInWorkspace();

        Task<ClientResponse<IsSlugUniqueResponse>> IsIdentifierUnique(string identifier);
    }
}
