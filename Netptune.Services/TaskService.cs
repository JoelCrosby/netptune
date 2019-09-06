using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
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

        public async Task<ServiceResult<TaskViewModel>> AddTask(ProjectTask projectTask, AppUser user)
        {
            var result = await _taskRepository.AddTask(projectTask, user);

            if (result == null) return ServiceResult<TaskViewModel>.BadRequest();

            await _unitOfWork.CompleteAsync();

            var viewModel = await _taskRepository.GetTaskViewModel(result.Id);

            return ServiceResult<TaskViewModel>.Ok(viewModel);
        }

        public async Task<ServiceResult<TaskViewModel>> DeleteTask(int id, AppUser user)
        {
            var result = await _taskRepository.DeleteTask(id, user);

            if (result == null) return ServiceResult<TaskViewModel>.BadRequest();

            var viewModel = await _taskRepository.GetTaskViewModel(result.Id);

            return ServiceResult<TaskViewModel>.Ok(viewModel);
        }

        public async Task<ServiceResult<ProjectTaskCounts>> GetProjectTaskCount(int projectId)
        {
            var result = await _taskRepository.GetProjectTaskCount(projectId);

            if (result == null) return ServiceResult<ProjectTaskCounts>.BadRequest();

            return ServiceResult<ProjectTaskCounts>.Ok(result);
        }

        public async Task<ServiceResult<TaskViewModel>> GetTask(int id)
        {
            var result = await _taskRepository.GetTaskViewModel(id);

            if (result == null) return ServiceResult<TaskViewModel>.BadRequest();

            return ServiceResult<TaskViewModel>.Ok(result);
        }

        public async Task<ServiceResult<IEnumerable<TaskViewModel>>> GetTasks(int workspaceId)
        {
            var result = await _taskRepository.GetTasksAsync(workspaceId);

            if (result == null) return ServiceResult<IEnumerable<TaskViewModel>>.BadRequest();

            return ServiceResult<IEnumerable<TaskViewModel>>.Ok(result);
        }

        public async Task<ServiceResult<TaskViewModel>> UpdateTask(ProjectTask projectTask, AppUser user)
        {
            var result = await _taskRepository.UpdateTask(projectTask, user);

            if (result == null) return ServiceResult<TaskViewModel>.BadRequest();

            var viewModel = await _taskRepository.GetTaskViewModel(result.Id);

            return ServiceResult<TaskViewModel>.Ok(viewModel);
        }
    }
}
