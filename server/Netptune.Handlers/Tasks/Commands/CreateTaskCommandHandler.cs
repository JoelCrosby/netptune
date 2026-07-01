using Mediator;
using Microsoft.EntityFrameworkCore;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Polly;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record CreateTaskCommand(AddProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CreateTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<TaskViewModel>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (!workspaceId.HasValue) return ClientResponse<TaskViewModel>.Failed($"workspace with key {workspaceKey} not found");

        var user = await Identity.GetCurrentUser();
        var userId = req.AssigneeId ?? user.Id;
        var project = await UnitOfWork.Projects.GetTaskCreationProject(req.ProjectId!.Value, workspaceId.Value, cancellationToken);

        if (project is null) return ClientResponse<TaskViewModel>.Failed($"Project with Id {req.ProjectId} not found");

        await UnitOfWork.Statuses.EnsureDefaultTaskStatuses(workspaceId.Value, user.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var status = await ResolveStatus(req, project, workspaceId.Value, cancellationToken);

        if (status is null) return ClientResponse<TaskViewModel>.Failed("Task status not found");

        var task = new ProjectTask
        {
            Name = req.Name,
            Description = req.Description,
            StatusId = status.Id,
            Status = status,
            ProjectId = req.ProjectId,
            SprintId = req.SprintId,
            OwnerId = user.Id,
            WorkspaceId = workspaceId.Value,
            Priority = req.Priority,
            EstimateType = req.EstimateType,
            EstimateValue = req.EstimateValue,
            ProjectTaskAppUsers = new List<ProjectTaskAppUser> { new() { UserId = userId } },
        };

        if (req.BoardGroupId.HasValue)
        {
            await AddTaskToBoardGroup(req.BoardGroupId.Value, task, cancellationToken);
        }
        else
        {
            await AddTaskToBoardGroup(project, task, cancellationToken);
        }

        var scopeIdRef = await UnitOfWork.Tasks.GetNextScopeId(project.Id, cancellationToken: cancellationToken);

        if (!scopeIdRef.HasValue) return ClientResponse<TaskViewModel>.Failed($"Unable to get scope id for project with id {project.Id}");

        var scopeId = scopeIdRef.Value;
        task.ProjectScopeId = scopeId;

        var result = await UnitOfWork.Tasks.AddAsync(task, cancellationToken);

        await Policy
            .Handle<DbUpdateException>()
            .Retry(4, (_, _, _) => scopeId++)
            .Execute(async () =>
            {
                result.ProjectScopeId = scopeId;
                return await UnitOfWork.CompleteAsync(cancellationToken);
            });

        var response = await UnitOfWork.Tasks.GetTaskViewModel(result.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Create;
        });

        return ClientResponse<TaskViewModel>.Success(response!);
    }

    private async Task AddTaskToBoardGroup(int groupId, ProjectTask task, CancellationToken cancellationToken)
    {
        var boardGroup = await UnitOfWork.BoardGroups.GetTaskTarget(groupId, cancellationToken);

        if (boardGroup is null) throw new Exception($"BoardGroup with id of {groupId} does not exist.");

        var status = await UnitOfWork.Statuses.GetFirstTaskStatusByCategory(
            task.WorkspaceId,
            boardGroup.Type.GetStatusCategoryFromGroupType(),
            cancellationToken);

        if (status is not null)
        {
            task.StatusId = status.Id;
            task.Status = status;
        }

        task.ProjectTaskInBoardGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = boardGroup.MaxSortOrder + 1,
            BoardGroupId = boardGroup.Id,
            ProjectTask = task,
        });
    }

    private async Task AddTaskToBoardGroup(TaskCreationProject project, ProjectTask task, CancellationToken cancellationToken)
    {
        var boardGroupType = task.Status.Category.GetGroupTypeFromStatusCategory();
        var boardGroup = await UnitOfWork.BoardGroups.GetDefaultTaskTarget(project.Id, boardGroupType, cancellationToken);

        if (boardGroup is null) throw new Exception($"Project '{project.Name}' With Id {project.Id} does not have a default board group of type {boardGroupType}.");

        task.ProjectTaskInBoardGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = boardGroup.MaxSortOrder + 1,
            BoardGroupId = boardGroup.Id,
            ProjectTask = task,
        });
    }

    private async Task<Status?> ResolveStatus(AddProjectTaskRequest request, TaskCreationProject project, int workspaceId, CancellationToken cancellationToken)
    {
        if (request.StatusId.HasValue)
        {
            return await UnitOfWork.Statuses.GetInWorkspace(request.StatusId.Value, workspaceId, cancellationToken: cancellationToken);
        }

        if (project.DefaultStatusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(project.DefaultStatusId.Value, workspaceId, cancellationToken: cancellationToken);
            if (status is not null) return status;
        }

        return await UnitOfWork.Statuses.GetTaskStatusByKey(workspaceId, "new", cancellationToken)
               ?? await UnitOfWork.Statuses.GetFirstTaskStatus(workspaceId, cancellationToken);
    }
}
