using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataPlane.Entites;
using DataPlane.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataPlane.Controllers {

    [Route ("api/[controller]")]
    [ApiController]
    public class WorkspacesController : ControllerBase {

        private readonly ProjectsContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public WorkspacesController (ProjectsContext context, UserManager<IdentityUser> userManager) {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Workspaces
        [HttpGet]
        public IEnumerable<Workspace> GetWorkspaces () {
            return _context.Workspaces;
        }

        // GET: api/Workspaces/5
        [HttpGet ("{id}")]
        public async Task<IActionResult> GetWorkspace ([FromRoute] int id) {
            if (!ModelState.IsValid) {
                return BadRequest (ModelState);
            }

            var workspace = await _context.Workspaces.FindAsync (id);

            if (workspace == null) {
                return NotFound ();
            }

            return Ok (workspace);
        }

        // PUT: api/Workspaces/5
        [HttpPut ("{id}")]
        public async Task<IActionResult> PutWorkspace ([FromRoute] int id, [FromBody] Workspace workspace) {
            if (!ModelState.IsValid) {
                return BadRequest (ModelState);
            }

            if (id != workspace.WorkspaceId) {
                return BadRequest ();
            }

            _context.Entry (workspace).State = EntityState.Modified;

            try {
                await _context.SaveChangesAsync ();
            } catch (DbUpdateConcurrencyException) {
                if (!WorkspaceExists (id)) {
                    return NotFound ();
                } else {
                    throw;
                }
            }

            return NoContent ();
        }

        // POST: api/Workspaces
        [HttpPost]
        public async Task<IActionResult> PostWorkspace ([FromBody] Workspace workspace) {
            if (!ModelState.IsValid) {
                return BadRequest (ModelState);
            }

            var userId = _userManager.GetUserId (HttpContext.User);

            workspace.CreatedByUserId = userId;
            workspace.OwnerId = userId;

            _context.Workspaces.Add (workspace);
            await _context.SaveChangesAsync ();

            return CreatedAtAction("GetWorkspace", new { id = workspace.WorkspaceId }, workspace);
        }

        // DELETE: api/Workspaces/5
        [HttpDelete ("{id}")]
        public async Task<IActionResult> DeleteWorkspace ([FromRoute] int id) {
            if (!ModelState.IsValid) {
                return BadRequest (ModelState);
            }

            var workspace = await _context.Workspaces.FindAsync (id);
            if (workspace == null) {
                return NotFound ();
            }

            _context.Workspaces.Remove (workspace);
            await _context.SaveChangesAsync ();

            return Ok (workspace);
        }

        private bool WorkspaceExists (int id) {
            return _context.Workspaces.Any (e => e.WorkspaceId == id);
        }
    }
}