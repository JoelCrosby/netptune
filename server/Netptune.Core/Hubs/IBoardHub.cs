using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Core.Hubs;

public interface IBoardHub
{
    Task JoinBoard(UserConnection? connection);

    Task LeaveBoard(UserConnection? connection);

    Task MoveTaskInBoardGroup(MoveTaskInGroupRequest request);

    Task Create(TaskViewModel request);

    Task Delete(ClientResponse response, int id);

    Task DeleteMultiple(ClientResponse response, IEnumerable<int> ids);

    Task Update(TaskViewModel request);

    Task UpdateGroup(BoardGroupViewModel request);

    Task AddTagToTask(TagViewModel response);

    Task DeleteTagFromTask(ClientResponse response);

    Task AddBoardGroup(ClientResponse response);

    Task DeleteBoardGroup(ClientResponse response);

    Task MoveTasksToGroup(ClientResponse response);

    Task ReassignTasks(ClientResponse response);
}
