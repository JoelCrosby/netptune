using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Services;

public interface IBoardGroupService
{
    Task<BoardGroup> GetBoardGroup(int id);

    Task<ClientResponse<BoardGroupViewModel>> UpdateBoardGroup(UpdateBoardGroupRequest request);

    Task<ClientResponse<BoardGroupViewModel>> AddBoardGroup(AddBoardGroupRequest boardGroup);

    Task<ClientResponse> Delete(int id);
}