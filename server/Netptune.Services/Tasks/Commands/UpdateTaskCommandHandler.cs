using Mediator;
using Microsoft.Extensions.Logging;
using Netptune.Core.Enums;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.Requests;

namespace Netptune.Services.Tasks.Commands;

public sealed record UpdateTaskCommand(UpdateProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;
    private readonly ILogger<UpdateTaskCommandHandler> Logger;

    public UpdateTaskCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity, ILogger<UpdateTaskCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        Logger = logger;
    }

    public async ValueTask<ClientResponse<TaskViewModel>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var result = await UnitOfWork.Tasks.GetAsync(req.Id);

        if (result is null) return ClientResponse<TaskViewModel>.NotFound;

        var old = result.ToViewModel() with { };

        await UnitOfWork.Transaction(async () =>
        {
            if (req.Status.HasValue && result.Status != req.Status.Value)
            {
                await PutTaskInBoardGroup(req.Status.Value, result);
            }

            result.Name = req.Name ?? result.Name;
            result.Description = req.Description ?? result.Description;
            result.Status = req.Status ?? result.Status;
            result.IsFlagged = req.IsFlagged ?? result.IsFlagged;
            result.OwnerId = req.OwnerId ?? result.OwnerId;

            if (req.AssigneeIds is not null)
            {
                result.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                    result.Id,
                    result.ProjectTaskAppUsers,
                    req.AssigneeIds).ToList();
            }

            await UnitOfWork.CompleteAsync();
        });

        var response = await UnitOfWork.Tasks.GetTaskViewModel(result.Id);

        if (response is null) return ClientResponse<TaskViewModel>.NotFound;

        ProjectTaskDiff.Create(old, response).LogDiff(Activity, response.Id);

        return ClientResponse<TaskViewModel>.Success(response);
    }

    private async Task PutTaskInBoardGroup(ProjectTaskStatus status, Core.Entities.ProjectTask result)
    {
        await RemoveTaskFromGroups(result.Id);
        await UnitOfWork.CompleteAsync();

        if (result.ProjectId is null) return;

        var defaultBoard = await UnitOfWork.Boards.GetDefaultBoardInProject(result.ProjectId.Value, false, true);

        if (defaultBoard is null)
        {
            Logger.LogInformation("Project With Id {ProjectId} does not have a default board", result.ProjectId.Value);
            return;
        }

        var groupType = status.GetGroupTypeFromTaskStatus();
        var group = defaultBoard.BoardGroups
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.SortOrder)
            .FirstOrDefault(item => item.Type == groupType);

        if (group is null) return;

        var sortOrder = group.TasksInGroups.LastOrDefault()?.SortOrder ?? 0 + 1;

        group.TasksInGroups.Add(new ProjectTaskInBoardGroup
        {
            BoardGroupId = group.Id,
            ProjectTaskId = result.Id,
            SortOrder = sortOrder,
        });
    }

    private async Task RemoveTaskFromGroups(int taskId)
    {
        var groupsWithTask = await UnitOfWork.BoardGroups.GetBoardGroupsForProjectTask(taskId);

        var taskInGroupsToDelete = groupsWithTask
            .Select(boardGroup => boardGroup.TasksInGroups.Where(x => x.ProjectTaskId == taskId))
            .SelectMany(taskInGroups => taskInGroups);

        await UnitOfWork.ProjectTasksInGroups.DeletePermanent(taskInGroupsToDelete);
        await UnitOfWork.CompleteAsync();
    }
}
