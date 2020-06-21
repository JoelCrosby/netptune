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
    public class UsersController : ControllerBase
    {
        private readonly IUserService UserService;

        public UsersController(IUserService userService)
        {
            UserService = userService;
        }

        // GET: api/AppUsers
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<AppUser>))]
        public async Task<IActionResult> GetWorkspaceUsersAsync(string workspaceSlug)
        {
            var result = await UserService.GetWorkspaceUsers(workspaceSlug);

            return Ok(result);
        }

        // GET: api/AppUsers/<guid>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(AppUser))]
        public async Task<IActionResult> GetUserAsync([FromRoute] string id)
        {
            var result = await UserService.Get(id);

            if (result is null) return NotFound();

            return Ok(result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(AppUser))]
        public async Task<IActionResult> UpdateUser([FromBody] AppUser user)
        {
            var result = await UserService.Update(user);

            if (result is null) return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [Route("Invite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(AppUser))]
        public async Task<IActionResult> Invite(string userId, int workspaceId)
        {
            var result = await UserService.InviteUserToWorkspace(userId, workspaceId);

            return Ok(result);
        }

        [HttpGet]
        [Route("GetUserByEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(AppUser))]
        public async Task<IActionResult> GetUserByEmailAsync(string email)
        {
            var result = await UserService.GetByEmail(email);

            if (result is null) return NotFound();

            return Ok(result);
        }
    }
}
