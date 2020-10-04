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

        Task<ProjectTask> GetTask(string systemId, string workspaceSlug);

        Task<int?> GetTaskInternalId(string systemId, string workspaceSlug);

        Task<TaskViewModel> GetTaskViewModel(string systemId, string workspaceSlug);

        Task<List<TaskViewModel>> GetTasksAsync(string workspaceSlug, bool isReadonly = false);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);

        Task<int?> GetNextScopeId(int projectId, int increment = 0);

        Task<List<ExportTaskViewModel>> GetExportTasksAsync(string workspaceSlug);

        Task<List<int>> GetTaskIdsInBoard(string boardIdentifier);
    }
}
