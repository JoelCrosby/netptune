using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Netptune.Models.Models;
using Netptune.Models.Repositories;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]
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
        public async Task<IActionResult> GetWorkspaceUsersAsync(int workspaceId)
        {
            var result = await _userRepository.GetWorkspaceUsersAsync(workspaceId);
            return result.ToRestResult();
        }

        // GET: api/AppUsers/<guid>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserAsync([FromRoute] string id)
        {
            var result = await _userRepository.GetUserAsync(id);
            return result.ToRestResult();
        }

        [HttpPost]
        [Route("UpdateUser")]
        public async Task<IActionResult> UpdateUser(AppUser user)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var result =  await _userRepository.UpdateUserAsync(user, userId);
            return result.ToRestResult();
        }

        [HttpPost]
        [Route("Invite")]
        public async Task<IActionResult> Invite(string userId, int workspaceId)
        {
            var result = await _userRepository.InviteUserToWorkspaceAsync(userId, workspaceId);
            return result.ToRestResult();
        }

        [HttpGet]
        [Route("GetUserByEmail")]
        public async Task<IActionResult> GetUserByEmailAsync(string email)
        {
            var result = await _userRepository.GetUserByEmailAsync(email);
            return result.ToRestResult();
        }
    }
}