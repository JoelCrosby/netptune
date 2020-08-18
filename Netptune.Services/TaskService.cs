using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Ordering;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Netptune.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository TaskRepository;
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IIdentityService IdentityService;

        public TaskService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService)
        {
            TaskRepository = unitOfWork.Tasks;
            UnitOfWork = unitOfWork;
            IdentityService = identityService;
        }

        public async Task<TaskViewModel> AddTask(AddProjectTaskRequest request)
        {
            var workspace = await UnitOfWork.Workspaces.GetBySlugWithTasks(request.Workspace, true);

            if (workspace is null) return null;

            var user = await IdentityService.GetCurrentUser();

            var sortOrder = request.SortOrder ?? GetSortOrder(workspace.ProjectTasks);

            var task = new ProjectTask
            {
                Name = request.Name,
                Description = request.Description,
                Status = request.Status ?? ProjectTaskStatus.New,
                SortOrder = sortOrder,
                ProjectId = request.ProjectId,
                AssigneeId = request.AssigneeId,
                OwnerId = user.Id,
                Workspace = workspace,
                WorkspaceId = workspace.Id,
            };

            var project = workspace.Projects.FirstOrDefault(item => !item.IsDeleted && item.Id == request.ProjectId);

            if (project is null) throw new Exception("ProjectId cannot be null.");

            if (request.BoardGroupId.HasValue)
            {
                await AddTaskToBoardGroup(request.BoardGroupId.Value, task, sortOrder);
            }
            else
            {
                await AddTaskToBoardGroup(project, task, sortOrder);
            }

            var result = await TaskRepository.AddAsync(task);

            var saveCounter = 0;

            async Task SaveTask(int increment)
            {
                if (saveCounter > 4) throw new Exception($"Save Task failed after {saveCounter + 1} attempts.");

                var scopeId = await GetNextProjectScopeId(project.Id, increment);

                if (!scopeId.HasValue) throw new Exception($"Unable to get scope id for project with id {project.Id}.");

                result.ProjectScopeId = scopeId.Value;

                try
                {
                    await UnitOfWork.CompleteAsync();
                }
                catch (Exception ex) when (ex is DBConcurrencyException || ex is DbUpdateException)
                {
                    saveCounter++;

                    await SaveTask(saveCounter);
                }
            }

            await SaveTask(saveCounter);

            return await TaskRepository.GetTaskViewModel(result.Id);
        }

        public async Task<TaskViewModel> DeleteTask(int id)
        {
            var task = await TaskRepository.GetAsync(id);
            var user = await IdentityService.GetCurrentUser();

            if (task is null) return null;

            task.IsDeleted = true;
            task.DeletedByUserId = user.Id;

            await RemoveTaskFromGroups(id);

            await UnitOfWork.CompleteAsync();

            return await TaskRepository.GetTaskViewModel(task.Id);
        }

        public Task<ProjectTaskCounts> GetProjectTaskCount(int projectId)
        {
            return TaskRepository.GetProjectTaskCount(projectId);
        }

        public Task<TaskViewModel> GetTask(int id)
        {
            return TaskRepository.GetTaskViewModel(id);
        }

        public Task<TaskViewModel> GetTask(string systemId, string workspaceSlug)
        {
            return TaskRepository.GetTaskViewModel(systemId, workspaceSlug);
        }

        public Task<List<TaskViewModel>> GetTasks(string workspaceSlug)
        {
            return TaskRepository.GetTasksAsync(workspaceSlug);
        }

        public async Task<TaskViewModel> UpdateTask(UpdateProjectTaskRequest request)
        {
            if (request?.Id is null) throw new ArgumentNullException(nameof(request));

            var result = await TaskRepository.GetAsync(request.Id.Value);

            if (result is null) return null;

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
                result.SortOrder = request.SortOrder ?? result.SortOrder;
                result.OwnerId = request.OwnerId;
                result.AssigneeId = request.AssigneeId;

                await UnitOfWork.CompleteAsync();
            });

            return await TaskRepository.GetTaskViewModel(result.Id);
        }

        private async Task PutTaskInBoardGroup(ProjectTaskStatus status, ProjectTask result)
        {
            await RemoveTaskFromGroups(result.Id);

            await UnitOfWork.CompleteAsync();

            if (result.ProjectId is null)
            {
                return;
            }

            var defaultBoard = await UnitOfWork.Boards.GetDefaultBoardInProject(result.ProjectId.Value, true);

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

        private static double GetSortOrder(IEnumerable<ProjectTask> projectTasks)
        {
            var largest = projectTasks.OrderByDescending(item => item.SortOrder).FirstOrDefault();

            if (largest is null) return 0;

            return largest.SortOrder + 1;
        }

        private async Task AddTaskToBoardGroup(int boardId, ProjectTask task, double sortOrder)
        {
            var boardGroup = await UnitOfWork.BoardGroups.GetAsync(boardId);

            if (boardGroup is null) throw new Exception($"BoardGroup with id of {boardId} does not exist.");

            task.Status = boardGroup.Type.GetTaskStatusFromGroupType();

            boardGroup.TasksInGroups.Add(new ProjectTaskInBoardGroup
            {
                SortOrder = sortOrder,
                BoardGroup = boardGroup,
                ProjectTask = task
            });
        }

        private async Task AddTaskToBoardGroup(Project project, ProjectTask task, double sortOrder)
        {
            var defaultBoard = await UnitOfWork.Boards.GetDefaultBoardInProject(project.Id, true);

            if (defaultBoard is null)
            {
                throw new Exception($"Project '{project.Name}' With Id {project.Id} does not have a default board.");
            }

            var boardGroupType = task.Status.GetGroupTypeFromTaskStatus();

            var boardGroup = defaultBoard.BoardGroups.FirstOrDefault(group => group.Type == boardGroupType);

            boardGroup?.TasksInGroups.Add(new ProjectTaskInBoardGroup
            {
                SortOrder = sortOrder,
                BoardGroup = boardGroup,
                ProjectTask = task
            });
        }

        public Task<ProjectTaskInBoardGroup> MoveTaskInBoardGroup(MoveTaskInGroupRequest request)
        {
            if (request.OldGroupId == request.NewGroupId)
            {
                return MoveTaskInGroup(request);
            }

            return TransferTaskInGroups(request);
        }

        private async Task<ProjectTaskInBoardGroup> TransferTaskInGroups(MoveTaskInGroupRequest request)
        {
            await UnitOfWork.Transaction(async () =>
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

                return newRelational;
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

            return taskInBoardGroup;
        }

        private async Task<ProjectTaskInBoardGroup> MoveTaskInGroup(MoveTaskInGroupRequest request)
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

            return item;
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

        private Task<int?> GetNextProjectScopeId(int projectId, int increment = 0)
        {
            return TaskRepository.GetNextScopeId(projectId, increment);
        }
    }
}
