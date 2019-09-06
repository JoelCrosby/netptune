using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories.Common;
using Netptune.Models;
using Netptune.Models.VeiwModels.ProjectTasks;

namespace Netptune.Core.Repositories
{
    public interface ITaskRepository : IRepository<ProjectTask, int>
    {
        Task<TaskViewModel> GetTaskViewModel(int workspaceId);

        Task<IEnumerable<TaskViewModel>> GetTasksAsync(int workspaceId);

        Task<ProjectTask> UpdateTask(ProjectTask projectTask, AppUser user);

        Task<ProjectTask> AddTask(ProjectTask projectTask, AppUser user);

        Task<ProjectTask> DeleteTask(int id, AppUser user);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);
    }
}