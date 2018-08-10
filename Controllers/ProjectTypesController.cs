using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataPlane.Data;
using DataPlane.Models;
using Microsoft.AspNetCore.Authorization;

namespace DataPlane.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectTypesController : ControllerBase
    {
        private readonly ProjectsContext _context;

        public ProjectTypesController(ProjectsContext context)
        {
            _context = context;
        }

        // GET: api/ProjectTypes
        [HttpGet]
        public IEnumerable<ProjectType> GetProjectTypes()
        {
            return _context.ProjectTypes;
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