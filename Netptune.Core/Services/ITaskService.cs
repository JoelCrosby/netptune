using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Models;
using Netptune.Models.VeiwModels.ProjectTasks;

namespace Netptune.Core.Services
{
    public interface ITaskService
    {
        Task<ServiceResult<IEnumerable<TaskViewModel>>> GetTasks(int workspaceId);

        Task<ServiceResult<TaskViewModel>> GetTask(int id);

        Task<ServiceResult<TaskViewModel>> UpdateTask(ProjectTask projectTask, AppUser user);

        Task<ServiceResult<TaskViewModel>> AddTask(ProjectTask projectTask, AppUser user);

        Task<ServiceResult<TaskViewModel>> DeleteTask(int id, AppUser user);

        Task<ServiceResult<ProjectTaskCounts>> GetProjectTaskCount(int projectId);
    }
}
