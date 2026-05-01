using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Repositories;

public interface ITaskRepository : IWorkspaceEntityRepository<ProjectTask, int>
{
    Task<TaskViewModel?> GetTaskViewModel(int id, CancellationToken cancellationToken = default);

    Task<ProjectTask?> GetTask(string systemId, string workspaceKey, CancellationToken cancellationToken = default);

    Task<int?> GetTaskInternalId(string systemId, string workspaceKey, CancellationToken cancellationToken = default);

    Task<TaskViewModel?> GetTaskViewModel(string systemId, string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<TaskViewModel>> GetTasksAsync(string workspaceKey, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<int?> GetNextScopeId(int projectId, int increment = 0, CancellationToken cancellationToken = default);

    Task<List<ExportTaskViewModel>> GetExportTasksAsync(string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<ExportTaskViewModel>> GetBoardExportTasksAsync(string workspaceKey, string boardIdentifier, CancellationToken cancellationToken = default);

    Task<List<int>> GetTaskIdsInBoard(string boardIdentifier, CancellationToken cancellationToken = default);
}
