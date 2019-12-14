using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Models;
using Netptune.Models.ViewModels.Projects;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class ProjectRepository : Repository<DataContext, Project, int>, IProjectRepository
    {
        public ProjectRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
            : base(dataContext, connectionFactories)
        {
        }

        public async Task<List<ProjectViewModel>> GetProjects(string workspaceSlug)
        {
            var workspace = await Context.Workspaces.FirstOrDefaultAsync(item => item.Slug == workspaceSlug);

            if (workspace is null) return new List<ProjectViewModel>();

            var workspaceId = workspace.Id;

            Entities.Include(task => task.Owner).ThenInclude(x => x.UserName);

            return await Entities
                .Where(project => project.WorkspaceId == workspaceId && !project.IsDeleted)
                .Include(project => project.Workspace)
                .Include(project => project.Owner)
                .Select(project => GetViewModel(project))
                .ToListAsync();
        }

        public Task<ProjectViewModel> GetProjectViewModel(int id)
        {
            Entities.Include(task => task.Owner).ThenInclude(x => x.UserName);

            return Entities
                .Where(project => project.Id == id)
                .Include(project => project.Workspace)
                .Include(project => project.Owner)
                .Select(project => GetViewModel(project))
                .FirstOrDefaultAsync();
        }

        private static ProjectViewModel GetViewModel(Project project)
        {
            return new ProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                RepositoryUrl = project.RepositoryUrl,
                WorkspaceId = project.WorkspaceId,
                OwnerDisplayName = project.Owner.GetDisplayName(),
                UpdatedAt = project.UpdatedAt,
                CreatedAt = project.CreatedAt
            };
        }

        public async Task<Project> AddProject(Project project)
        {
            var result = await Entities.AddAsync(project);

            return result.Entity;
        }

        public async Task<Project> DeleteProject(int id)
        {
            var result = await Entities.FindAsync(id);

            if (result is null)
            {
                return null;
            }

            Entities.Remove(result);

            return result;
        }
    }
}
