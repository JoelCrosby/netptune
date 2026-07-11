using Netptune.Core.Entities;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Core.Repositories;

public interface IProjectRepository : IWorkspaceEntityRepository<Project, int>
{
    Task<Project?> GetWithIncludes(int id, CancellationToken cancellationToken = default);

    Task<List<ProjectViewModel>> GetProjects(string workspaceKey, CancellationToken cancellationToken = default, PageRequest? pageRequest = null);

    Task<ProjectViewModel?> GetProjectViewModel(int id, CancellationToken cancellationToken = default);

    Task<List<ProjectViewModel>> GetProjectViewModels(IEnumerable<int> projectIds, CancellationToken cancellationToken = default);

    Task<List<ProjectViewModel>> GetAllProjectViewModels(string workspaceKey, CancellationToken cancellationToken = default);

    Task<ProjectViewModel?> GetProjectViewModel(string key, int workspaceId, CancellationToken cancellationToken = default);

    Task<TaskCreationProject?> GetTaskCreationProject(int projectId, int workspaceId, CancellationToken cancellationToken = default);

    Task<bool> IsProjectKeyAvailable(string key, int workspaceId, CancellationToken cancellationToken = default);

    Task<string> GenerateProjectKey(string projectName, int workspaceId, CancellationToken cancellationToken = default);
}
