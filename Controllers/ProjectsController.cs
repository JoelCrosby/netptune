using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataPlane.Entites;
using DataPlane.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using DataPlane.Models.Relationships;

namespace DataPlane.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectsContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProjectsController(ProjectsContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Projects
        [HttpGet]
        public IEnumerable<Project> GetProjects(int workspaceId)
        {
            _context.ProjectTasks.Include(x => x.Owner).ThenInclude(x => x.UserName);
            return _context.Projects.Where(x => x.WorkspaceId == workspaceId && x.IsDeleted != true)
                .Include(x => x.ProjectType).Include(x => x.Workspace).Include(x => x.Owner);
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            return Ok(project);
        }

        // PUT: api/Projects/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject([FromRoute] int id, [FromBody] Project project)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != project.ProjectId)
            {
                return BadRequest();
            }

            var modifiedProject = _context.Projects.SingleOrDefault(x => x.ProjectId == project.ProjectId);

            if (modifiedProject == null)
            {
                return BadRequest("Project not found");
            }

            modifiedProject.Name = project.Name;
            modifiedProject.Description = project.Description;
            modifiedProject.ProjectTypeId = project.ProjectTypeId;

            modifiedProject.ModifiedByUserId = _userManager.GetUserId(HttpContext.User);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(modifiedProject);
        }

        // POST: api/Projects
        [HttpPost]
        public async Task<IActionResult> PostProject([FromBody] Project project)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = _userManager.GetUserId(HttpContext.User);
            var workspace = _context.Workspaces.SingleOrDefault(x => x.WorkspaceId == project.WorkspaceId);
            var user = _context.AppUsers.SingleOrDefault(x => x.Id == userId);

            project.CreatedByUserId = userId;
            project.OwnerId = userId;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Need to explicily load the navigation propert context.
            // other wise the workspace.WorkspaceUsers list will return null.
            _context.Projects.Include(m => m.WorkspaceProjects);
            _context.Projects.Include(m => m.ProjectUsers);

            var workspaceRelationship = new WorkspaceProject();
            workspaceRelationship.Project = project;
            workspaceRelationship.Workspace = workspace;

            project.WorkspaceProjects.Add(workspaceRelationship);

            var userRelationship = new ProjectUser();
            userRelationship.Project = project;
            userRelationship.User = user;

            project.ProjectUsers.Add(userRelationship);

            await _context.SaveChangesAsync();

            return Ok(project);
        }

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok(project);
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }

    }
}