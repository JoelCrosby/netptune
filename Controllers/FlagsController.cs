using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataPlane.Entites;
using DataPlane.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace DataPlane.Controllers
{

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FlagsController : ControllerBase
    {
        private readonly ProjectsContext _context;
        private readonly UserManager<AppUser> _userManager;

        public FlagsController(ProjectsContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Flags
        [HttpGet]
        public IEnumerable<Flag> GetFlag()
        {
            return _context.Flags.Where(x => x.IsDeleted != true);;
        }

        // GET: api/Flags/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFlag([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var flag = await _context.Flags.FindAsync(id);

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

            var userId = _userManager.GetUserId (HttpContext.User);

            flag.CreatedByUserId = userId;
            flag.OwnerId = userId;

            _context.Flags.Add(flag);
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

            var flag = await _context.Flags.FindAsync(id);
            if (flag == null)
            {
                return NotFound();
            }

            _context.Flags.Remove(flag);
            await _context.SaveChangesAsync();

            return Ok(flag);
        }

        private bool FlagExists(int id)
        {
            return _context.Flags.Any(e => e.FlagId == id);
        }
    }
}