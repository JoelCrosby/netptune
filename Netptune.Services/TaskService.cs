using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.VeiwModels.ProjectTasks;

namespace Netptune.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly INetptuneUnitOfWork _unitOfWork;

        public TaskService(INetptuneUnitOfWork unitOfWork)
        {
            _taskRepository = unitOfWork.Tasks;
            _unitOfWork = unitOfWork;
        }

        public async Task<TaskViewModel> AddTask(ProjectTask projectTask, AppUser user)
        {
            var result = await _taskRepository.AddTask(projectTask, user);

            await _unitOfWork.CompleteAsync();

            return await _taskRepository.GetTaskViewModel(result.Id);
        }

        public async Task<TaskViewModel> DeleteTask(int id, AppUser user)
        {
            var result = await _taskRepository.DeleteTask(id, user);

            await _unitOfWork.CompleteAsync();

            return await _taskRepository.GetTaskViewModel(result.Id);
        }

        public Task<ProjectTaskCounts> GetProjectTaskCount(int projectId)
        {
            return _taskRepository.GetProjectTaskCount(projectId);
        }

        public Task<TaskViewModel> GetTask(int id)
        {
            return _taskRepository.GetTaskViewModel(id);
        }

        public Task<List<TaskViewModel>> GetTasks(int workspaceId)
        {
            return _taskRepository.GetTasksAsync(workspaceId);
        }

        public async Task<TaskViewModel> UpdateTask(ProjectTask projectTask, AppUser user)
        {
            var result = await _taskRepository.UpdateTask(projectTask, user);

            await _unitOfWork.CompleteAsync();

            return await _taskRepository.GetTaskViewModel(result.Id);
        }
    }
}
