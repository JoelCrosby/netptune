using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Services
{
    public interface IBoardGroupService
    {
        Task<BoardGroupsViewModel> GetBoardGroups(string boardIdentifier, BoardGroupsFilter filter = null);

        Task<BoardGroup> GetBoardGroup(int id);

        Task<BoardGroup> UpdateBoardGroup(BoardGroup boardGroup);

        Task<BoardGroup> AddBoardGroup(AddBoardGroupRequest boardGroup);

        Task<ClientResponse> Delete(int id);
    }
}
