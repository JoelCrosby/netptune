using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Entities;
using Netptune.Core.Services;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkspacesController : ControllerBase
    {
        private readonly IWorkspaceService WorkspaceService;

        public WorkspacesController(IWorkspaceService workspaceService)
        {
            WorkspaceService = workspaceService;
        }

        // GET: api/Workspaces
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<Workspace>))]
        public async Task<IActionResult> GetWorkspaces()
        {
            var result = await WorkspaceService.GetWorkspaces();

            return Ok(result);
        }

        // GET: api/Workspaces/slug
        [HttpGet("{slug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(Workspace))]
        public async Task<IActionResult> GetWorkspace([FromRoute] string slug)
        {
            var result = await WorkspaceService.GetWorkspace(slug);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // PUT: api/Workspaces/5
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(Workspace))]
        public async Task<IActionResult> PutWorkspace([FromBody] Workspace workspace)
        {
            var result = await WorkspaceService.UpdateWorkspace(workspace);

            return Ok(result);
        }

        // POST: api/Workspaces
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(Workspace))]
        public async Task<IActionResult> PostWorkspace([FromBody] Workspace workspace)
        {
            var result = await WorkspaceService.AddWorkspace(workspace);

            return Ok(result);
        }

        // DELETE: api/Workspaces/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(Workspace))]
        public async Task<IActionResult> DeleteWorkspace([FromRoute] int id)
        {
            var result = await WorkspaceService.DeleteWorkspace(id);

            if (result is null) return NotFound();

            return Ok(result);
        }
    }
}
