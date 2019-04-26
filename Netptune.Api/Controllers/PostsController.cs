using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netptune.Models.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netptune.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly Models.Entites.DataContext _context;

        public PostsController(Models.Entites.DataContext context)
        {
            _context = context;
        }

        // GET: api/Posts
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<Post>))]
        public IEnumerable<Post> GetPosts()
        {
            return _context.Posts.Where(x => !x.IsDeleted);
        }

        // GET: api/Posts/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(Post))]
        public async Task<IActionResult> GetPost([FromRoute] int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null || post.IsDeleted)
            {
                return NotFound();
            }

            return Ok(post);
        }

        // PUT: api/Posts/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(Post))]
        public async Task<IActionResult> PutPost([FromRoute] int id, [FromBody] Post post)
        {
            if (id != post.Id)
            {
                return BadRequest();
            }

            _context.Entry(post).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return Ok(post);
        }

        // POST: api/Posts
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [Produces("application/json", Type = typeof(Post))]
        public async Task<IActionResult> PostPost([FromBody] Post post)
        {
            post.Project = _context.Projects.FirstOrDefault(x => x.Id == post.ProjectId);

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPost", new { id = post.Id }, post);
        }

        // DELETE: api/Posts/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePost([FromRoute] int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet]
        [Route("GetProjectPosts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(List<Post>))]
        public IActionResult GetProjectPosts(int projectId)
        {
            var posts = _context.Posts.Where(x => x.ProjectId == projectId && !x.IsDeleted);
            if (posts == null)
            {
                return NotFound();
            }

            return Ok(posts);
        }
    }
}