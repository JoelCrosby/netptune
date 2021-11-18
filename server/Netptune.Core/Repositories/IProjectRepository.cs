using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Core.Repositories;

public interface IProjectRepository : IWorkspaceEntityRepository<Project, int>
{
    Task<List<ProjectViewModel>> GetProjects(string workspaceKey);

    Task<ProjectViewModel> GetProjectViewModel(int id);

    Task<ProjectViewModel> GetProjectViewModel(string key, int workspaceId);

    Task<bool> IsProjectKeyAvailable(string key, int workspaceId);

    Task<string> GenerateProjectKey(string projectName, int workspaceId);
}