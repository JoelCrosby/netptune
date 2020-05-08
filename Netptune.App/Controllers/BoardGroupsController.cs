using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core;
using Netptune.Core.Services;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BoardGroupsController : ControllerBase
    {
        private readonly IBoardGroupService BoardGroupService;
        private readonly UserManager<AppUser> UserManager;

        public BoardGroupsController(IBoardGroupService boardGroupService, UserManager<AppUser> userManager)
        {
            BoardGroupService = boardGroupService;
            UserManager = userManager;
        }

        // GET: api/boardgroups
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<BoardGroup>))]
        public async Task<IActionResult> GetBoardGroups(int boardId)
        {
            var result = await BoardGroupService.GetBoardGroups(boardId);

            return Ok(result);
        }

        // GET: api/boardgroups/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(BoardGroup))]
        public async Task<IActionResult> GetBoardGroup([FromRoute] int id)
        {
            var result = await BoardGroupService.GetBoardGroup(id);

            return Ok(result);
        }

        // PUT: api/boardgroups
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(BoardGroup))]
        public async Task<IActionResult> PutBoardGroup([FromBody] BoardGroup boardGroup)
        {
            var result = await BoardGroupService.UpdateBoardGroup(boardGroup);

            return Ok(result);
        }

        // POST: api/boardgroups
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(BoardGroup))]
        public async Task<IActionResult> PostBoardGroup([FromBody] BoardGroup boardGroup)
        {
            var result = await BoardGroupService.AddBoardGroup(boardGroup);

            return Ok(result);
        }

        // DELETE: api/boardgroups/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(BoardGroup))]
        public async Task<IActionResult> DeleteBoardGroup([FromRoute] int id)
        {
            var user = await UserManager.GetUserAsync(User);
            var result = await BoardGroupService.DeleteBoardGroup(id, user);

            return Ok(result);
        }
    }
}
