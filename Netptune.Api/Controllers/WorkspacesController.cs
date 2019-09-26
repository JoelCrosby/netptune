using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Netptune.Api.Extensions;
using Netptune.Core.Services;
using Netptune.Models;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkspacesController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly UserManager<AppUser> _userManager;

        public WorkspacesController(IWorkspaceService workspaceService, UserManager<AppUser> userManager)
        {
            _workspaceService = workspaceService;
            _userManager = userManager;
        }

        // GET: api/Workspaces
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<Workspace>))]
        public async Task<IActionResult> GetWorkspaces()
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _workspaceService.GetWorkspaces(user);

            return result.ToRestResult();
        }

        // GET: api/Workspaces/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(Workspace))]
        public async Task<IActionResult> GetWorkspace([FromRoute] int id)
        {
            var result = await _workspaceService.GetWorkspace(id);

            return result.ToRestResult();
        }

        // PUT: api/Workspaces/5
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(Workspace))]
        public async Task<IActionResult> PutWorkspace([FromBody] Workspace workspace)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _workspaceService.UpdateWorkspace(workspace, user);

            return result.ToRestResult();
        }

        // POST: api/Workspaces
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(Workspace))]
        public async Task<IActionResult> PostWorkspace([FromBody] Workspace workspace)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _workspaceService.AddWorkspace(workspace, user);

            return result.ToRestResult();
        }

        // DELETE: api/Workspaces/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWorkspace([FromRoute] int id)
        {
            var result = await _workspaceService.DeleteWorkspace(id);

            return result.ToRestResult();
        }
    }
}