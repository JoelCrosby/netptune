using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netptune.Models.Entites;
using Netptune.Models.Models;
using Netptune.Models.Models.Relationships;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspacesController : ControllerBase
    {
        private readonly Models.Entites.DataContext _context;
        private readonly UserManager<AppUser> _userManager;

        public WorkspacesController(Models.Entites.DataContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Workspaces
        [HttpGet]
        public async Task<IEnumerable<Workspace>> GetWorkspaces()
        {
            var user = await _userManager.GetUserAsync(User);

            // Load the relationship table.
            _context.Workspaces.Include(m => m.WorkspaceUsers).ThenInclude(e => e.User);

            // Select workspaces
            var workspaces = _context.WorkspaceAppUsers.Where(x => x.User.Id == user.Id).Select(w => w.Workspace);

            return workspaces.Where(x => !x.IsDeleted);
        }

        // GET: api/Workspaces/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWorkspace([FromRoute] int id)
        {

            var workspace = await _context.Workspaces.FindAsync(id);

            if (workspace == null) return NotFound();

            return Ok(workspace);
        }

        // PUT: api/Workspaces/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWorkspace([FromRoute] int id, [FromBody] Workspace workspace)
        {

            if (id != workspace.Id) return BadRequest();

            _context.Entry(workspace).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkspaceExists(id))
                    return NotFound();
            }

            return NoContent();
        }

        // POST: api/Workspaces
        [HttpPost]
        public async Task<IActionResult> PostWorkspace([FromBody] Workspace workspace)
        {

            var userId = _userManager.GetUserId(HttpContext.User);

            workspace.CreatedByUserId = userId;
            workspace.OwnerId = userId;

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);

            // Need to explicitly load the navigation property context.
            // other wise the workspace.WorkspaceUsers list will return null.
            _context.Workspaces.Include(m => m.WorkspaceUsers);

            var relationship = new WorkspaceAppUser
            {
                UserId = user.Id,
                WorkspaceId = workspace.Id
            };

            _context.WorkspaceAppUsers.Add(relationship);

            await _context.SaveChangesAsync();


            return CreatedAtAction("GetWorkspace", new {id = workspace.Id}, workspace);
        }

        // DELETE: api/Workspaces/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkspace([FromRoute] int id)
        {

            var workspace = await _context.Workspaces.FindAsync(id);
            if (workspace == null) return NotFound();

            _context.Workspaces.Remove(workspace);
            await _context.SaveChangesAsync();

            return Ok(workspace);
        }

        private bool WorkspaceExists(int id)
        {
            return _context.Workspaces.Any(e => e.Id == id);
        }
    }
}