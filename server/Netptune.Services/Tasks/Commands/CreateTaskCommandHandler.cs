using Mediator;
using Microsoft.EntityFrameworkCore;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Polly;

namespace Netptune.Services.Tasks.Commands;

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
        var workspace = await UnitOfWork.Workspaces.GetBySlugWithTasks(workspaceKey, true);

        if (workspace is null) return ClientResponse<TaskViewModel>.Failed($"workspace with key {workspaceKey} not found");

        var user = await Identity.GetCurrentUser();
        var userId = req.AssigneeId ?? user.Id;

        var task = new ProjectTask
        {
            Name = req.Name,
            Description = req.Description,
            Status = req.Status ?? ProjectTaskStatus.New,
            ProjectId = req.ProjectId,
            OwnerId = user.Id,
            WorkspaceId = workspace.Id,
            Priority = req.Priority,
            EstimateType = req.EstimateType,
            EstimateValue = req.EstimateValue,
            ProjectTaskAppUsers = new List<ProjectTaskAppUser> { new() { UserId = userId } },
        };

        var project = workspace.Projects.FirstOrDefault(item => !item.IsDeleted && item.Id == req.ProjectId);

        if (project is null) return ClientResponse<TaskViewModel>.Failed($"Project with Id {req.ProjectId} not found");

        if (req.BoardGroupId.HasValue)
        {
            await AddTaskToBoardGroup(req.BoardGroupId.Value, task);
        }
        else
        {
            await AddTaskToBoardGroup(project, task);
        }

        var result = await UnitOfWork.Tasks.AddAsync(task);

        var scopeIdRef = await UnitOfWork.Tasks.GetNextScopeId(project.Id);

        if (!scopeIdRef.HasValue) return ClientResponse<TaskViewModel>.Failed($"Unable to get scope id for project with id {project.Id}");

        var scopeId = scopeIdRef.Value;

        await Policy
            .Handle<DbUpdateException>()
            .Retry(4, (_, _, _) => scopeId++)
            .Execute(async () =>
            {
                result.ProjectScopeId = scopeId;
                return await UnitOfWork.CompleteAsync();
            });

        var response = await UnitOfWork.Tasks.GetTaskViewModel(result.Id);

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Create;
        });

        return ClientResponse<TaskViewModel>.Success(response!);
    }

    private async Task AddTaskToBoardGroup(int groupId, ProjectTask task)
    {
        var boardGroup = await UnitOfWork.BoardGroups.GetWithTasksInGroups(groupId);

        if (boardGroup is null) throw new Exception($"BoardGroup with id of {groupId} does not exist.");

        task.Status = boardGroup.Type.GetTaskStatusFromGroupType();

        var sortOrder = GetNextSortOrder(boardGroup);

        boardGroup.TasksInGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = sortOrder,
            BoardGroup = boardGroup,
            ProjectTask = task,
        });
    }

    private async Task AddTaskToBoardGroup(Project project, ProjectTask task)
    {
        var defaultBoard = await UnitOfWork.Boards.GetDefaultBoardInProject(project.Id, false, true);

        if (defaultBoard is null) throw new Exception($"Project '{project.Name}' With Id {project.Id} does not have a default board.");

        var boardGroupType = task.Status.GetGroupTypeFromTaskStatus();
        var boardGroup = defaultBoard.BoardGroups.FirstOrDefault(group => group.Type == boardGroupType);

        if (boardGroup is null) throw new Exception($"Board '{defaultBoard.Name}' With Id {defaultBoard.Id} does not have a group of type {boardGroupType}.");

        var sortOrder = GetNextSortOrder(boardGroup);

        boardGroup.TasksInGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = sortOrder,
            BoardGroup = boardGroup,
            ProjectTask = task,
        });
    }

    private static double GetNextSortOrder(BoardGroup boardGroup)
    {
        return boardGroup.TasksInGroups
            .OrderByDescending(t => t.SortOrder)
            .Select(t => t.SortOrder)
            .FirstOrDefault() + 1;
    }
}
