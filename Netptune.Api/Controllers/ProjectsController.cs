using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Services;
using Netptune.Models;
using Netptune.Models.Requests;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService ProjectService;
        private readonly UserManager<AppUser> UserManager;

        public ProjectsController(IProjectService projectService, UserManager<AppUser> userManager)
        {
            ProjectService = projectService;
            UserManager = userManager;
        }

        // GET: api/Projects/5
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<Project>))]
        public async Task<IActionResult> GetProjects(string workspaceSlug)
        {
            var result = await ProjectService.GetProjects(workspaceSlug);

            return Ok(result);
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(Project))]
        public async Task<IActionResult> GetProject([FromRoute] int id)
        {
            var result = await ProjectService.GetProject(id);

            return Ok(result);
        }

        // PUT: api/Projects
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(Project))]
        public async Task<IActionResult> PutProject([FromBody] Project project)
        {
            var user = await UserManager.GetUserAsync(User);
            var result = await ProjectService.UpdateProject(project, user);

            return Ok(result);
        }

        // POST: api/Projects
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(Project))]
        public async Task<IActionResult> PostProject([FromBody] AddProjectRequest request)
        {
            var user = await UserManager.GetUserAsync(User);
            var result = await ProjectService.AddProject(request, user);

            return Ok(result);
        }

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject([FromRoute] int id)
        {
            var user = await UserManager.GetUserAsync(User);
            var result = await ProjectService.DeleteProject(id, user);

            return Ok(result);
        }
    }
}