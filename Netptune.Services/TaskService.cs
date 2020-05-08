using Netptune.Core;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netptune.Services
{
    public class TaskService : ITaskService
    {
        protected readonly ITaskRepository TaskRepository;
        protected readonly INetptuneUnitOfWork UnitOfWork;

        public TaskService(INetptuneUnitOfWork unitOfWork)
        {
            TaskRepository = unitOfWork.Tasks;
            UnitOfWork = unitOfWork;
        }

        public async Task<TaskViewModel> AddTask(AddProjectTaskRequest request, AppUser user)
        {
            var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Workspace, true);

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
                WorkspaceId = workspace.Id
            };

            var project = workspace.Projects.FirstOrDefault(item => !item.IsDeleted && item.Id == request.ProjectId);

            if (project is null) throw new Exception("ProjectId cannot be null.");

            await AddTaskToBoardGroup(project, task, sortOrder);

            var result = await TaskRepository.AddAsync(task);

            await UnitOfWork.CompleteAsync();

            return await TaskRepository.GetTaskViewModel(result.Id);
        }

        private static double GetSortOrder(IEnumerable<ProjectTask> projectTasks)
        {
            var largest = projectTasks.OrderByDescending(item => item.SortOrder).FirstOrDefault();

            if (largest is null) throw new Exception("Could not determine sort order.");

            return largest.SortOrder + 1;
        }

        private async Task AddTaskToBoardGroup(Project project, ProjectTask task, double sortOrder)
        {
            var defaultBoard = await UnitOfWork.Boards.GetDefaultBoardInProject(project.Id, true);

            if (defaultBoard is null)
            {
                throw new Exception($"Project '{project.Name}' With Id {project.Id} does not have a default board.");
            }

            var boardGroupType = task.Status switch
            {
                ProjectTaskStatus.New => BoardGroupType.Todo,
                ProjectTaskStatus.InActive => BoardGroupType.Todo,
                ProjectTaskStatus.Complete => BoardGroupType.Done,
                ProjectTaskStatus.AwaitingClassification => BoardGroupType.Todo,
                ProjectTaskStatus.UnAssigned => BoardGroupType.Backlog,
                _ => BoardGroupType.Backlog
            };

            var boardGroup = defaultBoard.BoardGroups.FirstOrDefault(group => group.Type == boardGroupType);

            boardGroup?.TasksInGroups.Add(new ProjectTaskInBoardGroup
            {
                SortOrder = sortOrder,
                BoardGroup = boardGroup,
                ProjectTask = task
            });
        }

        public async Task<TaskViewModel> DeleteTask(int id, AppUser user)
        {
            var task = await TaskRepository.GetAsync(id);

            if (task is null) return null;

            task.IsDeleted = true;
            task.DeletedByUserId = user.Id;

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

        public Task<List<TaskViewModel>> GetTasks(string workspaceSlug)
        {
            return TaskRepository.GetTasksAsync(workspaceSlug);
        }

        public async Task<TaskViewModel> UpdateTask(ProjectTask projectTask)
        {
            if (projectTask is null) throw new ArgumentNullException(nameof(projectTask));

            var result = await TaskRepository.GetAsync(projectTask.Id);

            if (result is null) return null;

            result.Name = projectTask.Name;
            result.Description = projectTask.Description;
            result.Status = projectTask.Status;
            result.SortOrder = projectTask.SortOrder;
            result.OwnerId = projectTask.OwnerId;
            result.AssigneeId = projectTask.AssigneeId;

            await RemoveTaskFromGroups(projectTask.Id);

            await UnitOfWork.CompleteAsync();

            return await TaskRepository.GetTaskViewModel(result.Id);
        }

        public Task<ProjectTaskInBoardGroup> MoveTaskInBoardGroup(MoveTaskInGroupRequest request, AppUser user)
        {
            if (request.OldGroupId == request.NewGroupId)
            {
                return MoveTaskInGroup(request);
            }

            return TransferTaskInGroups(request);
        }

        private Task<ProjectTaskInBoardGroup> TransferTaskInGroups(MoveTaskInGroupRequest request)
        {
            return UnitOfWork.Transaction(async () =>
            {
                var oldGroup = await UnitOfWork.BoardGroups.GetAsync(request.OldGroupId);
                var itemToRemove = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, oldGroup.Id);

                if (itemToRemove is { })
                {
                    await UnitOfWork.ProjectTasksInGroups.DeletePermanent(itemToRemove.Id);
                }

                var tasks = await UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId);

                var existing = tasks
                    .Where(item => item.Id == request.TaskId)
                    .ToList();

                foreach (var item in existing)
                {
                    await UnitOfWork.ProjectTasksInGroups.DeletePermanent(item.Id);
                }

                var newGroup = await UnitOfWork.BoardGroups.GetAsync(request.NewGroupId);

                if (newGroup.Type == BoardGroupType.Done)
                {
                    var task = await UnitOfWork.Tasks.GetAsync(request.TaskId);

                    task.Status = ProjectTaskStatus.Complete;
                }

                var newRelational = new ProjectTaskInBoardGroup
                {
                    ProjectTaskId = request.TaskId,
                    BoardGroupId = request.NewGroupId,
                    SortOrder = request.SortOrder,
                };

                await UnitOfWork.ProjectTasksInGroups.AddAsync(newRelational);

                await UnitOfWork.CompleteAsync();

                return newRelational;
            });
        }

        private async Task<ProjectTaskInBoardGroup> MoveTaskInGroup(MoveTaskInGroupRequest request)
        {
            var relational = await UnitOfWork.BoardGroups.GetAsync(request.OldGroupId);

            var task = relational
                .TasksInGroups
                .FirstOrDefault(item => item.ProjectTaskId == request.TaskId);

            if (task is null)
            {
                return null;
            }

            task.SortOrder = request.SortOrder;

            await UnitOfWork.CompleteAsync();

            return task;
        }

        private async Task RemoveTaskFromGroups(int taskId)
        {
            var groupsWithTask = await UnitOfWork.BoardGroups.GetBoardGroupsForProjectTask(taskId);

            var taskInGroupsToDelete = groupsWithTask
                .Select(boardGroup => boardGroup.TasksInGroups.Where(x => x.ProjectTaskId == taskId))
                .SelectMany(taskInGroups => taskInGroups);

            foreach (var taskInGroup in taskInGroupsToDelete)
            {
                taskInGroup.IsDeleted = true;
            }
        }
    }
}
