using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Netptune.Entities.Entites;
using Netptune.Repository.Interfaces;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectRepository _projectRepository;
        private readonly UserManager<AppUser> _userManager;

        public ProjectsController(IProjectRepository projectRepository, UserManager<AppUser> userManager)
        {
            _projectRepository = projectRepository;
            _userManager = userManager;
        }

        // GET: api/Projects/5
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<Project>))]
        public async Task<IActionResult> GetProjects(int workspaceId)
        {
            var result = await _projectRepository.GetProjects(workspaceId);
            return result.ToRestResult();
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(Project))]
        public async Task<IActionResult> GetProject([FromRoute] int id)
        {
            var result = await _projectRepository.GetProject(id);
            return result.ToRestResult();
        }

        // PUT: api/Projects
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(Project))]
        public async Task<IActionResult> PutProject([FromBody] Project project)
        {
            var user = await _userManager.GetUserAsync(User) as AppUser;
            var result = await _projectRepository.UpdateProject(project, user);
            return result.ToRestResult();
        }

        // POST: api/Projects
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(Project))]
        public async Task<IActionResult> PostProject([FromBody] Project project)
        {
            var user = await _userManager.GetUserAsync(User) as AppUser;
            var result = await _projectRepository.AddProject(project, user);
            return result.ToRestResult();
        }

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject([FromRoute] int id)
        {
            var result = await _projectRepository.DeleteProject(id);
            return result.ToRestResult();
        }
    }
}