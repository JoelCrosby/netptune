using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Services;

public interface IBoardService
{
    Task<List<BoardViewModel>?> GetBoardsInProject(int projectId);

    Task<ClientResponse<BoardViewModel>> GetBoard(int id);

    Task<ClientResponse<BoardView>> GetBoardView(string boardIdentifier, BoardGroupsFilter? filter = null);

    Task<ClientResponse<BoardViewModel>> Update(UpdateBoardRequest request);

    Task<ClientResponse<BoardViewModel>> Create(AddBoardRequest request);

    Task<ClientResponse> Delete(int id);

    Task<List<BoardsViewModel>?> GetBoardsInWorkspace();

    Task<ClientResponse<IsSlugUniqueResponse>> IsIdentifierUnique(string identifier);
}
