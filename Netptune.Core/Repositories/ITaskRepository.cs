using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Repositories
{
    public interface ITaskRepository : IRepository<ProjectTask, int>
    {
        Task<TaskViewModel> GetTaskViewModel(int id);

        Task<List<TaskViewModel>> GetTasksAsync(string workspaceSlug, bool isReadonly = false);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);
    }
}
