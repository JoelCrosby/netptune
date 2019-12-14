using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories.Common;
using Netptune.Models;
using Netptune.Models.ViewModels.ProjectTasks;

namespace Netptune.Core.Repositories
{
    public interface ITaskRepository : IRepository<ProjectTask, int>
    {
        Task<TaskViewModel> GetTaskViewModel(int id);

        Task<List<TaskViewModel>> GetTasksAsync(string workspaceSlug);

        Task<ProjectTask> AddTask(ProjectTask projectTask, AppUser user);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);
    }
}