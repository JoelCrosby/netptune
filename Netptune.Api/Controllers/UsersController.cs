using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Netptune.Models.Entites;
using Netptune.Models.Models;
using Netptune.Models.Repositories;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        private readonly IUserRepository _userRepository;

        public UsersController(
            IConfiguration configuration,
            DataContext context,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IUserRepository userRepository
            )
        {
            _configuration = configuration;
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _userRepository = userRepository;
        }

        // GET: api/AppUsers
        [HttpGet]
        public IEnumerable<AppUser> GetWorkspaceUsers(int workspaceId)
        {
            return _userRepository.GetWorkspaceUsers(workspaceId);
        }

        // GET: api/AppUsers/<guid>
        [HttpGet("{id}")]
        public IActionResult GetUser([FromRoute] string id)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _userRepository.GetUser(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPost]
        [Route("UpdateUser")]
        public async Task<IActionResult> UpdateUser(AppUser user)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userRepository.UpdateUserAsync(user);

            return Ok(result);
        }

        [HttpPost]
        [Route("Invite")]
        public async Task<IActionResult> Invite(string userId, int workspaceId)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            

            return Ok(user);
        }

        [HttpGet]
        [Route("GetUserByEmail")]
        public IActionResult GetUserByEmail(string email)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _context.AppUsers.SingleOrDefault(x => x.Email == email);

            if (user == null)
            {
                return NotFound("user not found");
            }

            return Ok(user);
        }
    }
}