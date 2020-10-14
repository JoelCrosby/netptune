using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

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
        private readonly ITaskService TaskService;
        private readonly ITagService TagService;

        public BoardHub(IUserConnectionService userConnection, ITaskService taskService, ITagService tagsService)
        {
            UserConnection = userConnection;
            TaskService = taskService;
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

        public async Task AddToGroup(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            var userConnection = await UserConnection.Get(Context.ConnectionId);

            await Clients
                .OthersInGroup(group)
                .JoinBoard(userConnection);
        }

        public async Task RemoveFromGroup(string group)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);

            var userConnection = await UserConnection.Get(Context.ConnectionId);

            await Clients
                .OthersInGroup(group)
                .LeaveBoard(userConnection);
        }

        public async Task<ClientResponse> MoveTaskInBoardGroup(string group, MoveTaskInGroupRequest request)
        {
            if (IsInValidRequest(request)) return ClientResponse.Failed();

            var result = await TaskService.MoveTaskInBoardGroup(request);

            await Clients
                .OthersInGroup(group)
                .MoveTaskInBoardGroup(request);

            return result;
        }

        public async Task<ClientResponse<TaskViewModel>> Create(string group, AddProjectTaskRequest request)
        {
            if (IsInValidRequest(request)) return ClientResponse<TaskViewModel>.Failed();

            var result = await TaskService.Create(request);

            if (!result.IsSuccess) return result;

            await Clients
                .OthersInGroup(group)
                .Create(result.Payload);

            return result;
        }

        public async Task<ClientResponse> Delete(string group, int id)
        {
            var response = await TaskService.Delete(id);

            await Clients
                .OthersInGroup(group)
                .Delete(response, id);

            return response;
        }

        public async Task<ClientResponse> DeleteMultiple(string group, List<int> ids)
        {
            var response = await TaskService.Delete(ids);

            await Clients
                .OthersInGroup(group)
                .DeleteMultiple(response, ids);

            return response;
        }

        public async Task<ClientResponse<TaskViewModel>> Update(string group, UpdateProjectTaskRequest request)
        {
            if (IsInValidRequest(request)) return ClientResponse<TaskViewModel>.Failed();

            var result = await TaskService.Update(request);

            if (!result.IsSuccess) return result;

            await Clients
                .OthersInGroup(group)
                .Update(result.Payload);

            return result;
        }

        public async Task<ClientResponse<TagViewModel>> AddTagToTask(string group, AddTagRequest request)
        {
            if (IsInValidRequest(request)) return ClientResponse<TagViewModel>.Failed();

            var result = await TagService.AddTagToTask(request);

            if (!result.IsSuccess) return result;

            await Clients
                .OthersInGroup(group)
                .AddTagToTask(result.Payload);

            return result;
        }

        public async Task<ClientResponse> DeleteTagFromTask(string group, DeleteTagFromTaskRequest request)
        {
            if (IsInValidRequest(request)) return ClientResponse.Failed();

            var response = await TagService.DeleteFromTask(request);

            await Clients
                .OthersInGroup(group)
                .DeleteTagFromTask(response);

            return response;
        }

        private static bool IsInValidRequest(object target)
        {
            var context = new ValidationContext(target, null, null);
            var validationResults = new List<ValidationResult>();

            return !Validator.TryValidateObject(target, context, validationResults, true);
        }
    }
}
