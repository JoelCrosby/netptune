using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataPlane.Entites;
using DataPlane.Models;

namespace DataPlane.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlagsController : ControllerBase
    {
        private readonly ProjectsContext _context;

        public FlagsController(ProjectsContext context)
        {
            _context = context;
        }

        // GET: api/Flags
        [HttpGet]
        public IEnumerable<Flag> GetFlag()
        {
            return _context.Flag;
        }

        // GET: api/Flags/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFlag([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var flag = await _context.Flag.FindAsync(id);

            if (flag == null)
            {
                return NotFound();
            }

            return Ok(flag);
        }

        // PUT: api/Flags/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFlag([FromRoute] int id, [FromBody] Flag flag)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != flag.FlagId)
            {
                return BadRequest();
            }

            _context.Entry(flag).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlagExists(id))
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

        // POST: api/Flags
        [HttpPost]
        public async Task<IActionResult> PostFlag([FromBody] Flag flag)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Flag.Add(flag);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFlag", new { id = flag.FlagId }, flag);
        }

        // DELETE: api/Flags/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlag([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var flag = await _context.Flag.FindAsync(id);
            if (flag == null)
            {
                return NotFound();
            }

            _context.Flag.Remove(flag);
            await _context.SaveChangesAsync();

            return Ok(flag);
        }

        private bool FlagExists(int id)
        {
            return _context.Flag.Any(e => e.FlagId == id);
        }
    }
}