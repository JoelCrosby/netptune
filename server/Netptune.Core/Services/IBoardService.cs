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
        Task<List<Board>> GetBoards(int projectId);

        Task<Board> GetBoard(int id);

        Task<ClientResponse<BoardViewModel>> UpdateBoard(Board board);

        Task<ClientResponse<BoardViewModel>> AddBoard(AddBoardRequest board);

        Task<ClientResponse> Delete(int id);

        Task<List<BoardViewModel>> GetBoardsInWorkspace(string slug);

        Task<ClientResponse<IsSlugUniqueResponse>> IsIdentifierUnique(string identifier);
    }
}
