using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Hubs
{
    [Authorize]
    public class BoardHub : Hub
    {
        private readonly ITaskService TaskService;
        private readonly IIdentityService IdentityService;

        public BoardHub(ITaskService taskService, IIdentityService identityService)
        {
            TaskService = taskService;
            IdentityService = identityService;
        }

        public async Task MoveTaskInBoardGroup(MoveTaskInGroupRequest request)
        {
            await TaskService.MoveTaskInBoardGroup(request);

            await Clients.Others.SendAsync("moveTaskInBoardGroupReceived", request);
        }

        public async Task Create(AddProjectTaskRequest request)
        {
            IdentityService.BindHubUser(Context.User);

            var result = await TaskService.Create(request);

            await Clients.Others.SendAsync("createReceived", result);
        }

        public async Task Delete(int id)
        {
            var response = await TaskService.Delete(id);

            await Clients.Others.SendAsync("deleteReceived", response, id);
        }

        public async Task Update(UpdateProjectTaskRequest request)
        {
            var result = await TaskService.Update(request);

            await Clients.Others.SendAsync("updateReceived", result);
        }
    }
}
