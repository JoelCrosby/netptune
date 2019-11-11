﻿using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories.Common;
using Netptune.Models;
using Netptune.Models.VeiwModels.Projects;

namespace Netptune.Core.Repositories
{
    public interface IProjectRepository : IRepository<Project, int>
    {
        Task<List<ProjectViewModel>> GetProjects(int workspaceId);

        ValueTask<Project> GetProject(int id);

        Task<ProjectViewModel> GetProjectViewModel(int id);

        Task<Project> UpdateProject(Project project, AppUser user);

        Task<Project> AddProject(Project project, AppUser user);

        Task<Project> DeleteProject(int id);
    }
}
