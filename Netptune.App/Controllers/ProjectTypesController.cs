using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netptune.Entites;
using Netptune.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Netptune.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectTypesController : ControllerBase
    {
        private readonly ProjectsContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProjectTypesController(ProjectsContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/ProjectTypes
        [HttpGet]
        public IEnumerable<ProjectType> GetProjectTypes()
        {
            return _context.ProjectTypes.Where(x => !x.IsDeleted);
        }

        // GET: api/ProjectTypes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectType([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var projectType = await _context.ProjectTypes.FindAsync(id);

            if (projectType == null)
            {
                return NotFound();
            }

            return Ok(projectType);
        }

        // PUT: api/ProjectTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProjectType([FromRoute] int id, [FromBody] ProjectType projectType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != projectType.Id)
            {
                return BadRequest();
            }

            _context.Entry(projectType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectTypeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ProjectTypes
        [HttpPost]
        public async Task<IActionResult> PostProjectType([FromBody] ProjectType projectType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = _userManager.GetUserId (HttpContext.User);

            projectType.CreatedByUserId = userId;
            projectType.OwnerId = userId;

            _context.ProjectTypes.Add(projectType);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProjectType", new { id = projectType.Id }, projectType);
        }

        // DELETE: api/ProjectTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProjectType([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var projectType = await _context.ProjectTypes.FindAsync(id);
            if (projectType == null)
            {
                return NotFound();
            }

            _context.ProjectTypes.Remove(projectType);
            await _context.SaveChangesAsync();

            return Ok(projectType);
        }

        private bool ProjectTypeExists(int id)
        {
            return _context.ProjectTypes.Any(e => e.Id == id);
        }
    }
}