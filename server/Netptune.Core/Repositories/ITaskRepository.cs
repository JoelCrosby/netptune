using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Repositories
{
    public interface ITaskRepository : IWorkspaceEntityRepository<ProjectTask, int>
    {
        Task<TaskViewModel> GetTaskViewModel(int id);

        Task<ProjectTask> GetTask(string systemId, string workspaceKey);

        Task<int?> GetTaskInternalId(string systemId, string workspaceKey);

        Task<TaskViewModel> GetTaskViewModel(string systemId, string workspaceKey);

        Task<List<TaskViewModel>> GetTasksAsync(string workspaceKey, bool isReadonly = false);

        Task<ProjectTaskCounts> GetProjectTaskCount(int projectId);

        Task<int?> GetNextScopeId(int projectId, int increment = 0);

        Task<List<ExportTaskViewModel>> GetExportTasksAsync(string workspaceKey);

        Task<List<int>> GetTaskIdsInBoard(string boardIdentifier);

        Task<ActivityAncestors> GetAncestors(int taskId);
    }
}
