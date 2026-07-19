using Netptune.Core.Entities;
using Netptune.Core.Models.Search;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Repositories;

public interface ITaskRepository : IWorkspaceEntityRepository<ProjectTask, int>
{
    Task<TaskViewModel?> GetTaskViewModel(int id, CancellationToken cancellationToken = default);

    Task<List<TaskViewModel>> GetTaskViewModels(IEnumerable<int> taskIds, CancellationToken cancellationToken = default);

    Task<List<TaskSearchReference>> GetTaskSearchReferences(IEnumerable<int> taskIds, string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<TaskViewModel>> GetAllTaskViewModels(string workspaceKey, CancellationToken cancellationToken = default);

    Task<ProjectTask?> GetTaskForUpdate(int id, CancellationToken cancellationToken = default);

    Task<List<ProjectTask>> GetTasksForUpdate(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    Task<ProjectTask?> GetAutomationTask(int id, CancellationToken cancellationToken = default);

    Task<List<ProjectTask>> GetUnassignedAutomationCandidates(IReadOnlyCollection<int> workspaceIds, DateTime cutoff,CancellationToken cancellationToken = default);

    Task<ProjectTask?> GetTask(string systemId, string workspaceKey, CancellationToken cancellationToken = default);

    Task<ProjectTask?> GetTaskInWorkspace(string systemId, int workspaceId, CancellationToken cancellationToken = default);

    Task<int?> GetTaskInternalId(string systemId, string workspaceKey, CancellationToken cancellationToken = default);

    Task<TaskViewModel?> GetTaskViewModel(string systemId, string workspaceKey, CancellationToken cancellationToken = default);

    Task<PagedResponse<TaskViewModel>> GetTasksAsync(string workspaceKey, TaskFilter? filter = null, bool isReadonly = false, bool deleted = false, CancellationToken cancellationToken = default);

    Task<List<TaskStatusBreakdownItem>> GetTaskStatusBreakdownAsync(string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<ExportTaskViewModel>> GetExportTasksAsync(string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<ExportTaskViewModel>> GetBoardExportTasksAsync(string workspaceKey, string boardIdentifier, CancellationToken cancellationToken = default);

    Task<List<int>> GetTaskIdsInBoard(string boardIdentifier, CancellationToken cancellationToken = default);

    Task<int> UpdateTaskStatus(int id, int statusId, CancellationToken cancellationToken = default);

    Task<int> UpdateTaskStatuses(IEnumerable<int> ids, int statusId, CancellationToken cancellationToken = default);

    Task<List<int>> GetValidTaskIdsInWorkspace(IEnumerable<int> taskIds, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<int>> GetDeletedTaskIdsInWorkspace(IEnumerable<int> taskIds, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<int>> GetValidTaskIdsInProject(IEnumerable<int> taskIds, int workspaceId, int projectId, CancellationToken cancellationToken = default);

    Task AssignTasksToSprint(IEnumerable<int> taskIds, int sprintId, CancellationToken cancellationToken = default);

    Task AssignTasksToUser(IEnumerable<int> taskIds, string assigneeId, CancellationToken cancellationToken = default);
}
