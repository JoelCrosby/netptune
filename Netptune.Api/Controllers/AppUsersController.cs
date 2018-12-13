using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Netptune.Models.Entites;
using Netptune.Models.Models;
using Netptune.Models.Models.Relationships;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AppUsersController : ControllerBase
    {
        private readonly ProjectsContext _context;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public AppUsersController(
            IConfiguration configuration,
            ProjectsContext context,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager
            )
        {
            _configuration = configuration;
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: api/AppUsers
        [HttpGet]
        public IEnumerable<AppUser> GetWorkspaceUsers(int workspaceId)
        {

            var users = (from workspaceAppUsers in _context.WorkspaceAppUsers
                         where workspaceAppUsers.WorkspaceId == workspaceId
                         select workspaceAppUsers.User);
            return users;
        }

        // GET: api/AppUsers/<guid>
        [HttpGet("{id}")]
        public IActionResult GetUser([FromRoute] string id)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _context.Users.SingleOrDefault(x => x.Id == id);

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

            var updatedUser = _context.AppUsers.SingleOrDefault(x => x.Id == user.Id);

            if (updatedUser == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(HttpContext.User);

            if (userId != updatedUser.Id)
            {
                return Unauthorized();
            }

            updatedUser.PhoneNumber = user.PhoneNumber;

            updatedUser.FirstName = user.FirstName;
            updatedUser.LastName = user.LastName;

            if (updatedUser.Email != user.Email)
            {
                if (_context.AppUsers.Any(x => x.Email == user.Email))
                {
                    return BadRequest("Email address is already registered.");
                }

                updatedUser.Email = user.Email;
                updatedUser.UserName = user.UserName;
            }

            await _context.SaveChangesAsync();

            return Ok(updatedUser);


        }

        [HttpPost]
        [Route("Invite")]
        public async Task<IActionResult> Invite(string userId, int workspaceId)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = _context.AppUsers.SingleOrDefault(x => x.Id == userId);
                var workspace = _context.Workspaces.SingleOrDefault(x => x.Id == workspaceId);

                if (user == null)
                    return NotFound("user not found");

                if (workspace == null)
                    return NotFound("workspace not found");

                var alreadyExists = _context.WorkspaceAppUsers.Any(x => x.UserId == user.Id && x.WorkspaceId == workspace.Id);

                if (alreadyExists)
                    return BadRequest("User is already a member of the workspace");

                var invite = new WorkspaceAppUser()
                {
                    WorkspaceId = workspace.Id,
                    UserId = user.Id
                };

                _context.WorkspaceAppUsers.Add(invite);

                await _context.SaveChangesAsync();

                return Ok(user);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }

        [HttpGet]
        [Route("GetUserByEmail")]
        public IActionResult GetUserByEmail(string email)
        {

            try
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
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }
    }
}