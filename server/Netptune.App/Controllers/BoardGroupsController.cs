using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    public class BoardGroupsController : ControllerBase
    {
        private readonly IBoardGroupService BoardGroupService;

        public BoardGroupsController(IBoardGroupService boardGroupService)
        {
            BoardGroupService = boardGroupService;
        }

        // GET: api/boardgroups/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(BoardGroup))]
        public async Task<IActionResult> GetBoardGroup([FromRoute] int id)
        {
            var result = await BoardGroupService.GetBoardGroup(id);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // PUT: api/boardgroups
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(BoardGroup))]
        public async Task<IActionResult> PutBoardGroup([FromBody] UpdateBoardGroupRequest request)
        {
            var result = await BoardGroupService.UpdateBoardGroup(request);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // POST: api/boardgroups
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(ClientResponse<BoardGroupViewModel>))]
        public async Task<IActionResult> PostBoardGroup([FromBody] AddBoardGroupRequest request)
        {
            var result = await BoardGroupService.AddBoardGroup(request);

            return Ok(result);
        }

        // DELETE: api/boardgroups/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(ClientResponse))]
        public async Task<IActionResult> DeleteBoardGroup([FromRoute] int id)
        {
            var result = await BoardGroupService.Delete(id);

            if (result is null) return NotFound();

            return Ok(result);
        }
    }
}
