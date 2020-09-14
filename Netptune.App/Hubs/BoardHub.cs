using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Netptune.Core.Hubs;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.App.Hubs
{
    [Authorize]
    public class BoardHub : Hub<IBoardHub>
    {
        public const string Path = "/hubs/board-hub";

        private readonly ITaskService TaskService;

        public BoardHub(ITaskService taskService)
        {
            TaskService = taskService;
        }

        public async Task AddToBoard(string boardIdentifier)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, boardIdentifier);

            await Clients
                .OthersInGroup(boardIdentifier)
                .JoinBoard($"{Context.ConnectionId} has joined the board {boardIdentifier}.");
        }

        public async Task RemoveFromBoard(string boardIdentifier)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardIdentifier);

            await Clients
                .OthersInGroup(boardIdentifier)
                .LeaveBoard($"{Context.ConnectionId} has left the board {boardIdentifier}.");
        }

        public async Task<ClientResponse> MoveTaskInBoardGroup(string boardIdentifier, MoveTaskInGroupRequest request)
        {
            var result = await TaskService.MoveTaskInBoardGroup(request);

            await Clients
                .OthersInGroup(boardIdentifier)
                .MoveTaskInBoardGroup(request);

            return result;
        }

        public async Task<TaskViewModel> Create(string boardIdentifier, AddProjectTaskRequest request)
        {
            var result = await TaskService.Create(request);

            await Clients
                .OthersInGroup(boardIdentifier)
                .Create(result);

            return result;
        }

        public async Task<ClientResponse> Delete(string boardIdentifier, int id)
        {
            var response = await TaskService.Delete(id);

            await Clients
                .OthersInGroup(boardIdentifier)
                .Delete(response, id);

            return response;
        }

        public async Task<TaskViewModel> Update(string boardIdentifier, UpdateProjectTaskRequest request)
        {
            var result = await TaskService.Update(request);

            await Clients
                .OthersInGroup(boardIdentifier)
                .Update(result);

            return result;
        }
    }
}
