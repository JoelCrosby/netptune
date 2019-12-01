using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.ViewModels.ProjectTasks;

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

        public async Task<TaskViewModel> AddTask(ProjectTask projectTask, AppUser user)
        {
            var result = await TaskRepository.AddTask(projectTask, user);

            await UnitOfWork.CompleteAsync();

            return await TaskRepository.GetTaskViewModel(result.Id);
        }

        public async Task<TaskViewModel> DeleteTask(int id, AppUser user)
        {
            var result = await TaskRepository.DeleteTask(id, user);

            await UnitOfWork.CompleteAsync();

            return await TaskRepository.GetTaskViewModel(result.Id);
        }

        public Task<ProjectTaskCounts> GetProjectTaskCount(int projectId)
        {
            return TaskRepository.GetProjectTaskCount(projectId);
        }

        public Task<TaskViewModel> GetTask(int id)
        {
            return TaskRepository.GetTaskViewModel(id);
        }

        public Task<List<TaskViewModel>> GetTasks(int workspaceId)
        {
            return TaskRepository.GetTasksAsync(workspaceId);
        }

        public async Task<TaskViewModel> UpdateTask(ProjectTask projectTask, AppUser user)
        {
            var result = await TaskRepository.UpdateTask(projectTask, user);

            await UnitOfWork.CompleteAsync();

            return await TaskRepository.GetTaskViewModel(result.Id);
        }
    }
}
