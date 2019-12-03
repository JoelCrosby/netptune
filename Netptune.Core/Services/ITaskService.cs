using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Models;
using Netptune.Models.ViewModels.ProjectTasks;

namespace Netptune.Core.Services
{
    public interface ITaskService
    {
        Task<List<TaskViewModel>> GetTasks(int workspaceId);

        Task<TaskViewModel> GetTask(int id);

        Task<TaskViewModel> UpdateTask(ProjectTask projectTask);

        Task<TaskViewModel> AddTask(ProjectTask projectTask, AppUser user);

        Task<TaskViewModel> DeleteTask(int id, AppUser user);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);
    }
}
