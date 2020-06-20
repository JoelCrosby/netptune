﻿using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Core.Services
{
    public interface IProjectService
    {
        Task<List<ProjectViewModel>> GetProjects(string workspaceSlug);

        Task<ProjectViewModel> GetProject(int id);

        Task<ProjectViewModel> UpdateProject(Project project);

        Task<ProjectViewModel> AddProject(AddProjectRequest request);

        Task<ProjectViewModel> DeleteProject(int id);
    }
}
