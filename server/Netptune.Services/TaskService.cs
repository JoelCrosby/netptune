using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Ordering;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

using Polly;

namespace Netptune.Services;

public class TaskService : ServiceBase<TaskViewModel>, ITaskService
{
    private readonly ITaskRepository TaskRepository;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly ILogger<TaskService> Logger;

    public TaskService(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        ILogger<TaskService> logger)
    {
        TaskRepository = unitOfWork.Tasks;
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        Logger = logger;
    }

    public async Task<ClientResponse<TaskViewModel>> Create(AddProjectTaskRequest request)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await UnitOfWork.Workspaces.GetBySlugWithTasks(workspaceKey, true);

        if (workspace is null)
        {
            return Failed($"workspace with key {workspaceKey} not found");
        }

        var user = await Identity.GetCurrentUser();
        var userId = request.AssigneeId ?? user.Id;

        var task = new ProjectTask
        {
            Name = request.Name,
            Description = request.Description,
            Status = request.Status ?? ProjectTaskStatus.New,
            ProjectId = request.ProjectId,
            OwnerId = user.Id,
            WorkspaceId = workspace.Id,
            ProjectTaskAppUsers = new List<ProjectTaskAppUser>
            {
                new ()
                {
                    UserId = userId,
                },
            },
        };

        var project = workspace.Projects.FirstOrDefault(item => !item.IsDeleted && item.Id == request.ProjectId);

        if (project is null)
        {
            return Failed($"Project with Id {request.ProjectId} not found");
        }

        if (request.BoardGroupId.HasValue)
        {
            await AddTaskToBoardGroup(request.BoardGroupId.Value, task);
        }
        else
        {
            await AddTaskToBoardGroup(project, task);
        }

        var result = await TaskRepository.AddAsync(task);

        var scopeIdRef = await TaskRepository.GetNextScopeId(project.Id);

        if (!scopeIdRef.HasValue)
        {
            return Failed($"Unable to get scope id for project with id {project.Id}");
        }

        var scopeId = scopeIdRef.Value;

        await Policy
            .Handle<DbUpdateException>()
            .Retry(4, (_, _, _) => scopeId++)
            .Execute(async () =>
            {
                result.ProjectScopeId = scopeId;
                return await UnitOfWork.CompleteAsync();
            });

        var response = await TaskRepository.GetTaskViewModel(result.Id);

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Create;
        });

        return Success(response!);
    }

    public Task<TaskViewModel?> GetTask(int id)
    {
        return TaskRepository.GetTaskViewModel(id);
    }

    public Task<TaskViewModel?> GetTaskDetail(string systemId)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        return TaskRepository.GetTaskViewModel(systemId, workspaceKey);
    }

    public Task<List<TaskViewModel>> GetTasks()
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        return TaskRepository.GetTasksAsync(workspaceKey);
    }

    public async Task<ClientResponse<TaskViewModel>> Update(UpdateProjectTaskRequest request)
    {
        if (request.Id is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var result = await TaskRepository.GetAsync(request.Id.Value);

        if (result is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        var old = result.ToViewModel() with {};

        await UnitOfWork.Transaction(async () =>
        {
            if (request.Status.HasValue && result.Status != request.Status.Value)
            {
                await PutTaskInBoardGroup(request.Status.Value, result);
            }

            result.Name = request.Name ?? result.Name;
            result.Description = request.Description ?? result.Description;
            result.Status = request.Status ?? result.Status;
            result.IsFlagged = request.IsFlagged ?? result.IsFlagged;
            result.OwnerId = request.OwnerId ?? result.OwnerId;

            if (request.AssigneeIds is {})
            {
                result.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                        result.Id,
                        result.ProjectTaskAppUsers,
                        request.AssigneeIds)
                    .ToList();
            }

            await UnitOfWork.CompleteAsync();
        });

        var response = await TaskRepository.GetTaskViewModel(result.Id);
        if (response is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        ProjectTaskDiff
            .Create(old, response)
            .LogDiff(Activity, response.Id);

        return Success(response);
    }

    public async Task<ClientResponse> MoveTasksToGroup(MoveTasksToGroupRequest request)
    {
        var boardGroup = await UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value);

        if (boardGroup is null) return ClientResponse.Failed();

        var taskIdsInBoard = await TaskRepository.GetTaskIdsInBoard(request.BoardId);
        var taskIds = request.TaskIds.Where(id => taskIdsInBoard.Contains(id)).ToList();

        await RemoveTaskFromGroups(taskIds);

        var baseSortOrder = boardGroup.TasksInGroups
            .OrderByDescending(task => task.SortOrder)
            .Select(task => task.SortOrder)
            .FirstOrDefault() + 1;

        var taskInGroups = taskIds.Select((id, index) => new ProjectTaskInBoardGroup
        {
            BoardGroupId = boardGroup.Id,
            ProjectTaskId = id,
            SortOrder = baseSortOrder + index,
        });

        await UnitOfWork.ProjectTasksInGroups.AddRangeAsync(taskInGroups);
        await UnitOfWork.CompleteAsync();

        Activity.LogWithMany<MoveTaskActivityMeta>(options =>
        {
            options.EntityIds = taskIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta
            {
                Group = boardGroup.Name,
                GroupId = boardGroup.Id,
            };
        });

        return ClientResponse.Success();
    }

    public async Task<ClientResponse> ReassignTasks(ReassignTasksRequest request)
    {
        var taskIdsInBoard = await TaskRepository.GetTaskIdsInBoard(request.BoardId);
        var taskIds = request.TaskIds.Where(id => taskIdsInBoard.Contains(id)).ToList();

        var tasks = await UnitOfWork.Tasks.GetAllByIdAsync(taskIds);

        foreach (var task in tasks)
        {
            // TODO: Allow changing assignees

            task.UpdatedAt = DateTime.UtcNow;
            task.ProjectTaskAppUsers.Add(new ProjectTaskAppUser
            {
                UserId = request.AssigneeId,
            });
        }

        await UnitOfWork.CompleteAsync();

        Activity.LogWithMany<AssignActivityMeta>(options =>
        {
            options.EntityIds = taskIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Assign;
            options.Meta = new AssignActivityMeta
            {
                AssigneeId = request.AssigneeId,
            };
        });

        return ClientResponse.Success();
    }

    public Task<ClientResponse> MoveTaskInBoardGroup(MoveTaskInGroupRequest request)
    {
        if (request.OldGroupId == request.NewGroupId)
        {
            return MoveTaskInGroup(request);
        }

        return TransferTaskInGroups(request);
    }

    public async Task<ClientResponse> Delete(int id)
    {
        var task = await TaskRepository.GetAsync(id);

        if (task is null) return ClientResponse.NotFound;

        await TaskRepository.DeletePermanent(task.Id);
        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = task.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success();
    }

    public async Task<ClientResponse> Delete(IEnumerable<int> ids)
    {
        var tasks = await TaskRepository.GetAllByIdAsync(ids);
        var taskIds = tasks.ConvertAll(task => task.Id);

        await RemoveTaskFromGroups(taskIds);
        await TaskRepository.DeletePermanent(taskIds);
        await UnitOfWork.CompleteAsync();

        Activity.LogMany(options =>
        {
            options.EntityIds = taskIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success();
    }

    private async Task PutTaskInBoardGroup(ProjectTaskStatus status, ProjectTask result)
    {
        await RemoveTaskFromGroups(result.Id);

        await UnitOfWork.CompleteAsync();

        if (result.ProjectId is null)
        {
            return;
        }

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

        if (group is null)
        {
            return;
        }

        var sortOrder = group.TasksInGroups.LastOrDefault()?.SortOrder ?? 0 + 1;

        group.TasksInGroups.Add(new ProjectTaskInBoardGroup
        {
            BoardGroupId = group.Id,
            ProjectTaskId = result.Id,
            SortOrder = sortOrder,
        });
    }

    private async Task AddTaskToBoardGroup(int groupId, ProjectTask task)
    {
        var boardGroup = await UnitOfWork.BoardGroups.GetWithTasksInGroups(groupId);

        if (boardGroup is null) throw new Exception($"BoardGroup with id of {groupId} does not exist.");

        task.Status = boardGroup.Type.GetTaskStatusFromGroupType();

        var sortOrder = GetNextTaskInGroupSortOrder(boardGroup);

        boardGroup.TasksInGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = sortOrder,
            BoardGroup = boardGroup,
            ProjectTask = task,
        });
    }

    private static double GetNextTaskInGroupSortOrder(BoardGroup boardGroup)
    {
        var max = boardGroup.TasksInGroups
            .OrderByDescending(taskInGroup => taskInGroup.SortOrder)
            .Select(taskInGroup => taskInGroup.SortOrder)
            .FirstOrDefault();

        return max + 1;
    }

    private async Task AddTaskToBoardGroup(Project project, ProjectTask task)
    {
        var defaultBoard = await UnitOfWork.Boards.GetDefaultBoardInProject(project.Id, false, true);

        if (defaultBoard is null)
        {
            throw new Exception($"Project '{project.Name}' With Id {project.Id} does not have a default board.");
        }

        var boardGroupType = task.Status.GetGroupTypeFromTaskStatus();
        var boardGroup = defaultBoard.BoardGroups.FirstOrDefault(group => group.Type == boardGroupType);

        if (boardGroup is null)
        {
            throw new Exception($"Board '{defaultBoard.Name}' With Id {defaultBoard.Id} does not have a group of type {boardGroupType}.");
        }

        var sortOrder = GetNextTaskInGroupSortOrder(boardGroup);

        boardGroup.TasksInGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = sortOrder,
            BoardGroup = boardGroup,
            ProjectTask = task,
        });
    }

    private async Task<ClientResponse> TransferTaskInGroups(MoveTaskInGroupRequest request)
    {
        var boardGroup = await UnitOfWork.Transaction(async () =>
        {
            var itemToRemove = await UnitOfWork
                .ProjectTasksInGroups
                .GetProjectTaskInGroup(request.TaskId, request.OldGroupId);

            if (itemToRemove is { })
            {
                await UnitOfWork.ProjectTasksInGroups.DeletePermanent(itemToRemove.Id);
            }

            var tasks = await UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId);

            var existing = tasks
                .Where(x => x.Id == request.TaskId)
                .ToList();

            await UnitOfWork.ProjectTasksInGroups.DeletePermanent(existing.Select(item => item.Id));

            var newGroup = await UnitOfWork.BoardGroups.GetAsync(request.NewGroupId);
            var task = await UnitOfWork.Tasks.GetAsync(request.TaskId);

            if (newGroup is null || task is null)
            {
                return null;
            }

            task.Status = newGroup.Type.GetTaskStatusFromGroupType();

            var newRelational = new ProjectTaskInBoardGroup
            {
                ProjectTaskId = request.TaskId,
                BoardGroupId = request.NewGroupId,
                SortOrder = -1,
            };

            await UnitOfWork.ProjectTasksInGroups.AddAsync(newRelational);

            await UnitOfWork.CompleteAsync();

            return newGroup;
        });

        var taskInBoardGroup = await UnitOfWork
            .ProjectTasksInGroups
            .GetProjectTaskInGroup(request.TaskId, request.NewGroupId);

        var sortOrder = await GetTaskInGroupSortOrder(
            request.NewGroupId,
            request.TaskId,
            request.PreviousIndex,
            request.CurrentIndex,
            true);

        if (boardGroup is null || taskInBoardGroup is null)
        {
            return ClientResponse.NotFound;
        }

        taskInBoardGroup.SortOrder = sortOrder;

        await UnitOfWork.CompleteAsync();

        Activity.LogWith<MoveTaskActivityMeta>(options =>
        {
            options.EntityId = request.TaskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Move;
            options.Meta = new MoveTaskActivityMeta
            {
                Group = boardGroup.Name,
                GroupId = boardGroup.Id,
            };
        });

        return ClientResponse.Success();
    }

    private async Task<ClientResponse> MoveTaskInGroup(MoveTaskInGroupRequest request)
    {
        var item = await UnitOfWork
            .ProjectTasksInGroups
            .GetProjectTaskInGroup(request.TaskId, request.NewGroupId);

        if (item is null) return ClientResponse.NotFound;

        item.SortOrder = await GetTaskInGroupSortOrder(
            request.NewGroupId,
            request.TaskId,
            request.PreviousIndex,
            request.CurrentIndex);

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = request.TaskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success();
    }

    private async Task<double> GetTaskInGroupSortOrder
        (int groupId, int taskId, int previousIndex, int currentIndex, bool isNewItem = false)
    {
        var tasks = await UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(groupId);
        var item = tasks.Find(task => task.ProjectTaskId == taskId);

        if (item is null)
        {
            throw new ($"Task with id of {taskId} does not exist in group {groupId}.");
        }

        if (currentIndex < 0 || currentIndex > tasks.Count)
        {
            throw new ($"Get task in group sort order request '{nameof(currentIndex)}' is outside range of board group");
        }

        tasks.RemoveAt(!isNewItem ? previousIndex : 0);
        tasks.Insert(currentIndex, item);

        var preIndex = currentIndex - 1;
        var nextIndex = currentIndex + 1;

        var preOrder = tasks.ElementAtOrDefault(preIndex)?.SortOrder;
        var nextOrder = tasks.ElementAtOrDefault(nextIndex)?.SortOrder;

        return OrderingUtils.GetNewSortOrder(preOrder, nextOrder);
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

    private async Task RemoveTaskFromGroups(IEnumerable<int> taskIds)
    {
        var ids = await UnitOfWork.ProjectTasksInGroups.GetAllByTaskId(taskIds);
        await UnitOfWork.ProjectTasksInGroups.DeletePermanent(ids);
    }
}
