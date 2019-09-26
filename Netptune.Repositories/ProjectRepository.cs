using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Models.VeiwModels.Projects;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class ProjectRepository : Repository<DataContext, Project, int>, IProjectRepository
    {
        public ProjectRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
            : base(dataContext, connectionFactories)
        {
        }

        public async Task<IEnumerable<ProjectViewModel>> GetProjects(int workspaceId)
        {
            Context.ProjectTasks.Include(task => task.Owner).ThenInclude(x => x.UserName);

            return await Context.Projects
                .Where(project => project.WorkspaceId == workspaceId && !project.IsDeleted)
                .Include(project => project.Workspace)
                .Include(project => project.Owner)
                .Select(project => GetViewModel(project))
                .ToListAsync();
        }

        public async Task<Project> GetProject(int id)
        {
            return await Context.Projects.FindAsync(id);
        }

        public async Task<ProjectViewModel> GetProjectViewModel(int id)
        {
            Context.ProjectTasks.Include(task => task.Owner).ThenInclude(x => x.UserName);

            return await Context.Projects
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

        public async Task<Project> UpdateProject(Project project, AppUser user)
        {
            var result = await Context.Projects.FirstOrDefaultAsync(x => x.Id == project.Id);

            if (result == null)
            {
                return null;
            }

            result.Name = project.Name;
            result.Description = project.Description;

            result.ModifiedByUserId = user.Id;

            return result;
        }

        public async Task<Project> AddProject(Project project, AppUser user)
        {
            var workspace = await Context.Workspaces.FirstOrDefaultAsync(x => x.Id == project.WorkspaceId);

            project.CreatedByUserId = user.Id;
            project.OwnerId = user.Id;

            var result = await Context.Projects.AddAsync(project);

            await Context.SaveChangesAsync();

            // Need to explicily load the navigation property context.
            // other wise the workspace.WorkspaceUsers list will return null.
            Context.Projects.Include(m => m.WorkspaceProjects);
            Context.Projects.Include(m => m.ProjectUsers);

            var workspaceRelationship = new WorkspaceProject
            {
                ProjectId = project.Id,
                WorkspaceId = workspace.Id
            };

            project.WorkspaceProjects.Add(workspaceRelationship);

            var userRelationship = new ProjectUser
            {
                ProjectId = project.Id,
                UserId = user.Id
            };

            project.ProjectUsers.Add(userRelationship);

            return result.Entity;
        }

        public async Task<Project> DeleteProject(int id)
        {
            var result = await Context.Projects.FindAsync(id);

            if (result == null)
            {
                return null;
            }

            Context.Projects.Remove(result);

            return result;
        }
    }
}
