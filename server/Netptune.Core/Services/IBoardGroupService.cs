using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Core.Services
{
    public interface IBoardGroupService
    {
        Task<BoardGroup> GetBoardGroup(int id);

        Task<BoardGroup> UpdateBoardGroup(BoardGroup boardGroup);

        Task<BoardGroup> AddBoardGroup(AddBoardGroupRequest boardGroup);

        Task<ClientResponse> Delete(int id);
    }
}
