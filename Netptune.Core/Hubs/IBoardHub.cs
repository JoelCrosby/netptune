using System.Threading.Tasks;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Hubs
{
    public interface IBoardHub
    {

        Task JoinBoard(UserConnection connection);

        Task LeaveBoard(UserConnection connection);

        Task MoveTaskInBoardGroup(MoveTaskInGroupRequest request);

        Task Create(TaskViewModel request);

        Task Delete(ClientResponse response, int id);

        Task Update(TaskViewModel request);
    }
}
