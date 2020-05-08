using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.ProjectTasks;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories
{
    public interface ITaskRepository : IRepository<ProjectTask, int>
    {
        Task<TaskViewModel> GetTaskViewModel(int id);

        Task<List<TaskViewModel>> GetTasksAsync(string workspaceSlug);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);
    }
}