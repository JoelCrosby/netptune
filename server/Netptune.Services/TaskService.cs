using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Ordering;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

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

        public async Task<TaskViewModel> Create(AddProjectTaskRequest request)
        {
            var workspace = await UnitOfWork.Workspaces.GetBySlugWithTasks(request.Workspace, true);

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

            var saveCounter = 0;

            async Task SaveTask(int increment)
            {
                if (saveCounter > 4) throw new Exception($"Save Task failed after {saveCounter + 1} attempts.");

                var scopeId = await TaskRepository.GetNextScopeId(project.Id, increment);

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

        public async Task<ClientResponse> Delete(int id)
        {
            var task = await TaskRepository.GetAsync(id);
            var userId = await IdentityService.GetCurrentUserId();

            if (task is null || userId is null) return null;

            await TaskRepository.DeletePermanent(task.Id);
            await UnitOfWork.CompleteAsync();

            return ClientResponse.Success();
        }

        public async Task<ClientResponse> Delete(IEnumerable<int> ids)
        {
            if (ids is null) throw new ArgumentNullException(nameof(ids));

            var tasks = await TaskRepository.GetAllByIdAsync(ids);
            var taskIds = tasks.Select(task => task.Id).ToList();

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

        public Task<TaskViewModel> GetTaskDetail(string systemId, string workspaceSlug)
        {
            return TaskRepository.GetTaskViewModel(systemId, workspaceSlug);
        }

        public Task<List<TaskViewModel>> GetTasks(string workspaceSlug)
        {
            return TaskRepository.GetTasksAsync(workspaceSlug);
        }

        public async Task<TaskViewModel> Update(UpdateProjectTaskRequest request)
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
                result.OwnerId = request.OwnerId;
                result.AssigneeId = request.AssigneeId;

                await UnitOfWork.CompleteAsync();
            });

            return await TaskRepository.GetTaskViewModel(result.Id);
        }

        public async Task<ClientResponse> MoveTasksToGroup(MoveTasksToGroupRequest request)
        {
            await TaskRepository.GetTaskIdsInBoard(request.BoardId);

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
                ProjectTask = task
            });
        }

        private static double GetNextTaskInGroupSortOrder(BoardGroup boardGroup)
        {
            return boardGroup.TasksInGroups
                            .OrderByDescending(taskInGroup => taskInGroup.SortOrder)
                            .Select(taskInGroup => taskInGroup.SortOrder)
                            .FirstOrDefault();
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
                ProjectTask = task
            });
        }

        public Task<ClientResponse> MoveTaskInBoardGroup(MoveTaskInGroupRequest request)
        {
            if (request.OldGroupId == request.NewGroupId)
            {
                return MoveTaskInGroup(request);
            }

            return TransferTaskInGroups(request);
        }

        private async Task<ClientResponse> TransferTaskInGroups(MoveTaskInGroupRequest request)
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
    }
}
