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
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserRepository _userRepository;

        public UsersController(UserManager<AppUser> userManager, IUserRepository userRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
        }

        // GET: api/AppUsers
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<AppUser>))]
        public async Task<IActionResult> GetWorkspaceUsersAsync(int workspaceId)
        {
            var result = await _userRepository.GetWorkspaceUsersAsync(workspaceId);

            return result.ToRestResult();
        }

        // GET: api/AppUsers/<guid>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(AppUser))]
        public async Task<IActionResult> GetUserAsync([FromRoute] string id)
        {
            var result = await _userRepository.GetUserAsync(id);

            return result.ToRestResult();
        }

        [HttpPost]
        [Route("UpdateUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(AppUser))]
        public async Task<IActionResult> UpdateUser(AppUser user)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var result = await _userRepository.UpdateUserAsync(user, userId);

            return result.ToRestResult();
        }

        [HttpPost]
        [Route("Invite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(AppUser))]
        public async Task<IActionResult> Invite(string userId, int workspaceId)
        {
            var result = await _userRepository.InviteUserToWorkspaceAsync(userId, workspaceId);

            return result.ToRestResult();
        }

        [HttpGet]
        [Route("GetUserByEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(AppUser))]
        public async Task<IActionResult> GetUserByEmailAsync(string email)
        {
            var result = await _userRepository.GetUserByEmailAsync(email);

            return result.ToRestResult();
        }
    }
}