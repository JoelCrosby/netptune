using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netptune.Entities.Contexts;
using Netptune.Entities.Entites;
using Netptune.Entities.Entites.Relationships;
using Netptune.Repositories.Models;
using Netptune.Repository.Interfaces;

namespace Netptune.Repository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly DataContext _context;

        public ProjectRepository(DataContext dataContext)
        {
            _context = dataContext;
        }

        public async Task<RepoResult<IEnumerable<Project>>> GetProjects(int workspaceId)
        {
            _context.ProjectTasks.Include(x => x.Owner).ThenInclude(x => x.UserName);

            var result = await _context.Projects
                .Where(x => x.WorkspaceId == workspaceId && !x.IsDeleted)
                .Include(x => x.Workspace)
                .Include(x => x.Owner)
                .ToListAsync();

            return RepoResult<IEnumerable<Project>>.Ok(result);
        }

        public async Task<RepoResult<Project>> GetProject(int id)
        {
            var result = await _context.Projects.FindAsync(id);

            if (result == null)
            {
                return RepoResult<Project>.NotFound();
            }

            return RepoResult<Project>.Ok(result);
        }

        public async Task<RepoResult<Project>> UpdateProject(Project project, AppUser user)
        {
            var result = _context.Projects.SingleOrDefault(x => x.Id == project.Id);

            if (result == null)
            {
                return RepoResult<Project>.BadRequest();
            }

            result.Name = project.Name;
            result.Description = project.Description;

            result.ModifiedByUserId = user.Id;

            await _context.SaveChangesAsync();

            return RepoResult<Project>.Ok(result);
        }

        public async Task<RepoResult<Project>> AddProject(Project project, AppUser user)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var workspace = _context.Workspaces.SingleOrDefault(x => x.Id == project.WorkspaceId);

                    project.CreatedByUserId = user.Id;
                    project.OwnerId = user.Id;

                    var result = await _context.Projects.AddAsync(project);
                    await _context.SaveChangesAsync();

                    // Need to explicily load the navigation property context.
                    // other wise the workspace.WorkspaceUsers list will return null.
                    _context.Projects.Include(m => m.WorkspaceProjects);
                    _context.Projects.Include(m => m.ProjectUsers);

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

                    await _context.SaveChangesAsync();

                    transaction.Commit();

                    return RepoResult<Project>.Ok(result.Entity);
                }
                catch (System.Exception)
                {
                    throw;
                }
            }
        }

        public async Task<RepoResult<Project>> DeleteProject(int id)
        {
            var result = await _context.Projects.FindAsync(id);
            if (result == null)
            {
                return RepoResult<Project>.NotFound();
            }

            _context.Projects.Remove(result);
            await _context.SaveChangesAsync();

            return RepoResult<Project>.NoContent();
        }
    }
}
