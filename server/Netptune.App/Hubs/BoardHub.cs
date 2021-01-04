using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

using Netptune.Core.Entities;
using Netptune.Core.Hubs;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.App.Hubs
{
    [Authorize]
    public class BoardHub : Hub<IBoardHub>
    {
        public const string Path = "/hubs/board-hub";

        private readonly IUserConnectionService UserConnection;
        private readonly IHttpContextAccessor ContextAccessor;
        private readonly ITaskService TaskService;
        private readonly IBoardGroupService BoardGroupService;
        private readonly ITagService TagService;

        public BoardHub(
            IUserConnectionService userConnection,
            IHttpContextAccessor contextAccessor,
            ITaskService taskService,
            IBoardGroupService boardGroupService,
            ITagService tagsService)
        {
            UserConnection = userConnection;
            ContextAccessor = contextAccessor;
            TaskService = taskService;
            BoardGroupService = boardGroupService;
            TagService = tagsService;
        }

        public override async Task OnConnectedAsync()
        {
            await UserConnection.Add(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await UserConnection.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task AddToGroup(HubRequest request)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, request.Group);

            var userConnection = await UserConnection.Get(Context.ConnectionId);

            await Clients
                .OthersInGroup(request.Group)
                .JoinBoard(userConnection);
        }

        public async Task RemoveFromGroup(HubRequest request)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, request.Group);

            var userConnection = await UserConnection.Get(Context.ConnectionId);

            await Clients
                .OthersInGroup(request.Group)
                .LeaveBoard(userConnection);
        }

        public async Task<ClientResponse> MoveTaskInBoardGroup(HubRequest<MoveTaskInGroupRequest> request)
        {
            SetupHttpContext(request);

            if (IsInValidRequest(request.Payload)) return ClientResponse.Failed();

            var result = await TaskService.MoveTaskInBoardGroup(request.Payload);

            await Clients
                .OthersInGroup(request.Group)
                .MoveTaskInBoardGroup(request.Payload);

            return result;
        }

        public async Task<ClientResponse<TaskViewModel>> Create(HubRequest<AddProjectTaskRequest> request)
        {
            SetupHttpContext(request);

            if (IsInValidRequest(request.Payload)) return ClientResponse<TaskViewModel>.Failed();

            var result = await TaskService.Create(request.Payload);

            if (!result.IsSuccess) return result;

            await Clients
                .OthersInGroup(request.Group)
                .Create(result.Payload);

            return result;
        }

        public async Task<ClientResponse> Delete(HubRequest<int> request)
        {
            SetupHttpContext(request);

            var response = await TaskService.Delete(request.Payload);

            await Clients
                .OthersInGroup(request.Group)
                .Delete(response, request.Payload);

            return response;
        }

        public async Task<ClientResponse> DeleteMultiple(HubRequest<List<int>> request)
        {
            SetupHttpContext(request);

            var response = await TaskService.Delete(request.Payload);

            await Clients
                .OthersInGroup(request.Group)
                .DeleteMultiple(response, request.Payload);

            return response;
        }

        public async Task<ClientResponse<TaskViewModel>> Update(HubRequest<UpdateProjectTaskRequest> request)
        {
            SetupHttpContext(request);

            if (IsInValidRequest(request.Payload)) return ClientResponse<TaskViewModel>.Failed();

            var result = await TaskService.Update(request.Payload);

            if (!result.IsSuccess) return result;

            await Clients
                .OthersInGroup(request.Group)
                .Update(result.Payload);

            return result;
        }

        public async Task<ClientResponse<BoardGroup>> UpdateGroup(HubRequest<UpdateBoardGroupRequest> request)
        {
            SetupHttpContext(request);

            if (IsInValidRequest(request.Payload)) return ClientResponse<BoardGroup>.Failed();

            var result = await BoardGroupService.UpdateBoardGroup(request.Payload);

            if (!result.IsSuccess) return result;

            await Clients
                .OthersInGroup(request.Group)
                .UpdateGroup(result.Payload);

            return result;
        }

        public async Task<ClientResponse<TagViewModel>> AddTagToTask(HubRequest<AddTagToTaskRequest> request)
        {
            SetupHttpContext(request);

            if (IsInValidRequest(request.Payload)) return ClientResponse<TagViewModel>.Failed();

            var result = await TagService.AddTagToTask(request.Payload);

            if (!result.IsSuccess) return result;

            await Clients
                .OthersInGroup(request.Group)
                .AddTagToTask(result.Payload);

            return result;
        }

        public async Task<ClientResponse> DeleteTagFromTask(HubRequest<DeleteTagFromTaskRequest> request)
        {
            SetupHttpContext(request);

            if (IsInValidRequest(request.Payload)) return ClientResponse.Failed();

            var response = await TagService.DeleteFromTask(request.Payload);

            await Clients
                .OthersInGroup(request.Group)
                .DeleteTagFromTask(response);

            return response;
        }

        public async Task<ClientResponse<BoardGroup>> AddBoardGroup(HubRequest<AddBoardGroupRequest> request)
        {
            SetupHttpContext(request);

            if (IsInValidRequest(request.Payload)) return ClientResponse<BoardGroup>.Failed();

            var response = await BoardGroupService.AddBoardGroup(request.Payload);

            await Clients
                .OthersInGroup(request.Group)
                .AddBoardGroup(response);

            return response;
        }

        public async Task<ClientResponse> DeleteBoardGroup(HubRequest<int> request)
        {
            SetupHttpContext(request);

            var response = await BoardGroupService.Delete(request.Payload);

            await Clients
                .OthersInGroup(request.Group)
                .DeleteBoardGroup(response);

            return response;
        }

        public async Task<ClientResponse> MoveTasksToGroup(HubRequest<MoveTasksToGroupRequest> request)
        {
            SetupHttpContext(request);

            var response = await TaskService.MoveTasksToGroup(request.Payload);

            await Clients
                .OthersInGroup(request.Group)
                .MoveTasksToGroup(response);

            return response;
        }

        public async Task<ClientResponse> ReassignTasks(HubRequest<ReassignTasksRequest> request)
        {
            SetupHttpContext(request);

            var response = await TaskService.ReassignTasks(request.Payload);

            await Clients
                .OthersInGroup(request.Group)
                .ReassignTasks(response);

            return response;
        }

        private static bool IsInValidRequest(object target)
        {
            var context = new ValidationContext(target, null, null);
            var validationResults = new List<ValidationResult>();

            return !Validator.TryValidateObject(target, context, validationResults, true);
        }

        private void SetupHttpContext<T>(HubRequest<T> request)
        {
            ContextAccessor.HttpContext?.Request.Headers.TryAdd("workspace", request.WorkspaceKey);
        }
    }
}
