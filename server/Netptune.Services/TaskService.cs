using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Ordering;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

using Polly;

namespace Netptune.Services
{
    public class TaskService : ServiceBase<TaskViewModel>, ITaskService
    {
        private readonly ITaskRepository TaskRepository;
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IIdentityService IdentityService;
        private readonly IActivityLogger Activity;

        public TaskService(
            INetptuneUnitOfWork unitOfWork,
            IIdentityService identityService,
            IActivityLogger activity)
        {
            TaskRepository = unitOfWork.Tasks;
            UnitOfWork = unitOfWork;
            IdentityService = identityService;
            Activity = activity;
        }

        public async Task<ClientResponse<TaskViewModel>> Create(AddProjectTaskRequest request)
        {
            var workspaceKey = IdentityService.GetWorkspaceKey();
            var workspace = await UnitOfWork.Workspaces.GetBySlugWithTasks(workspaceKey, true);

            if (workspace is null) return null;

            var user = await IdentityService.GetCurrentUser();

            var task = new ProjectTask
            {
                Name = request.Name,
                Description = request.Description,
                Status = request.Status ?? ProjectTaskStatus.New,
                ProjectId = request.ProjectId,
                AssigneeId = request.AssigneeId ?? user.Id,
                OwnerId = user.Id,
                WorkspaceId = workspace.Id,
            };

            var project = workspace.Projects.FirstOrDefault(item => !item.IsDeleted && item.Id == request.ProjectId);

            if (project is null) throw new Exception("ProjectId cannot be null.");

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
                throw new Exception($"Unable to get scope id for project with id {project.Id}.");
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

            return Success(response);
        }

        public async Task<ClientResponse> Delete(int id)
        {
            var task = await TaskRepository.GetAsync(id);
            var userId = await IdentityService.GetCurrentUserId();

            if (task is null || userId is null) return null;

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
            if (ids is null) throw new ArgumentNullException(nameof(ids));

            var tasks = await TaskRepository.GetAllByIdAsync(ids);
            var taskIds = tasks.ConvertAll(task => task.Id);

            await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId(taskIds);
            await TaskRepository.DeletePermanent(taskIds);
            await UnitOfWork.CompleteAsync();

            return ClientResponse.Success();
        }

        public Task<ProjectTaskCounts> GetProjectTaskCount(int projectId)
        {
            return TaskRepository.GetProjectTaskCount(projectId);
        }

        public Task<TaskViewModel> GetTask(int id)
        {
            return TaskRepository.GetTaskViewModel(id);
        }

        public Task<TaskViewModel> GetTaskDetail(string systemId)
        {
            var workspaceKey = IdentityService.GetWorkspaceKey();
            return TaskRepository.GetTaskViewModel(systemId, workspaceKey);
        }

        public Task<List<TaskViewModel>> GetTasks()
        {
            var workspaceKey = IdentityService.GetWorkspaceKey();
            return TaskRepository.GetTasksAsync(workspaceKey);
        }

        public async Task<ClientResponse<TaskViewModel>> Update(UpdateProjectTaskRequest request)
        {
            if (request?.Id is null) throw new ArgumentNullException(nameof(request));

            var result = await TaskRepository.GetAsync(request.Id.Value);

            if (result is null) return null;

            var reassigned = request.AssigneeId != result.AssigneeId;

            await UnitOfWork.Transaction(async () =>
            {
                if (request.Status.HasValue && result.Status != request.Status.Value)
                {
                    await PutTaskInBoardGroup(request.Status.Value, result);
                }

                result.Name = request.Name;
                result.Description = request.Description;
                result.Status = request.Status ?? result.Status;
                result.IsFlagged = request.IsFlagged ?? result.IsFlagged;
                result.OwnerId = request.OwnerId;
                result.AssigneeId = request.AssigneeId;

                await UnitOfWork.CompleteAsync();
            });

            var response = await TaskRepository.GetTaskViewModel(result.Id);

            Activity.Log(options =>
            {
                options.EntityId = response.Id;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.Modify;
            });

            if (reassigned)
            {
                Activity.LogWith<AssignActivityMeta>(options =>
                {
                    options.EntityId = response.Id;
                    options.EntityType = EntityType.Task;
                    options.Type = ActivityType.Assign;
                    options.Meta = new AssignActivityMeta
                    {
                        AssigneeId = request.AssigneeId,
                    };
                });
            }

            return Success(response);
        }

        public async Task<ClientResponse> MoveTasksToGroup(MoveTasksToGroupRequest request)
        {
            var boardGroup = await UnitOfWork.BoardGroups.GetAsync(request.NewGroupId);

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
                task.AssigneeId = request.AssigneeId;
                task.UpdatedAt = DateTime.UtcNow;
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
                throw new Exception($"Project With Id {result.ProjectId.Value} does not have a default board.");
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
            var sortOrder = GetNextTaskInGroupSortOrder(boardGroup);

            boardGroup?.TasksInGroups.Add(new ProjectTaskInBoardGroup
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

            if (item is null) return null;

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
            var item = tasks.FirstOrDefault(task => task.ProjectTaskId == taskId);

            if (item is null)
            {
                throw new Exception($"Task with id of {taskId} does not exist in group {groupId}.");
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
            var taskInGroupsToDelete = await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId(taskIds);

            await UnitOfWork.ProjectTasksInGroups.DeletePermanent(taskInGroupsToDelete);
            await UnitOfWork.CompleteAsync();
        }
    }
}
