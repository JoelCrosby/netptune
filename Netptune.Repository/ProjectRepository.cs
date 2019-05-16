using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Netptune.Models.Entites;
using Netptune.Models.Contexts;
using Netptune.Models.Entites.Relationships;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netptune.Repositories.Models;
using Netptune.Repository.Interfaces;

namespace Netptune.Repository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProjectRepository(DataContext dataContext, UserManager<AppUser> userManager)
        {
            _context = dataContext;
            _userManager = userManager;
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


                return RepoResult<Project>.Ok(result.Entity);
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
