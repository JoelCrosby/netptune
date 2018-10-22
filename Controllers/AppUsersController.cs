using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Netptune.Entites;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Netptune.Models;

namespace Netptune.Controllers
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

            var users = (from WorkspaceAppUsers in _context.WorkspaceAppUsers
                            where
                                WorkspaceAppUsers.WorkspaceId == workspaceId
                            select WorkspaceAppUsers.User);
            return users;
        }

        // GET: api/AppUsers/<guid>
        [HttpGet ("{id}")]
        public IActionResult GetUser([FromRoute] string id) {

            if (!ModelState.IsValid) {
                return BadRequest (ModelState);
            }

            var user = _context.Users.SingleOrDefault(x => x.Id == id);

            if (user == null) {
                return NotFound ();
            }

            return Ok (user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(AppUser user) {

            try
            {
                if (!ModelState.IsValid) {
                return BadRequest (ModelState);
                }

                var updatedUser = _context.AppUsers.SingleOrDefault(x => x.Id == user.Id);

                if (updatedUser == null) {
                    return NotFound();
                }

                updatedUser.PhoneNumber = user.PhoneNumber;
                
                updatedUser.FirstName = user.FirstName;
                updatedUser.LastName = user.LastName;

                if (updatedUser.UserName != user.UserName)
                {
                    if(_context.AppUsers.Any(x => x.UserName == user.UserName))
                    {
                        return BadRequest("Username is already taken.");
                    }

                    updatedUser.UserName = user.UserName;
                }

                if (updatedUser.Email != user.Email)
                {
                    if(_context.AppUsers.Any(x => x.Email == user.Email))
                    {
                        return BadRequest("Email address is already registered.");
                    }

                    updatedUser.Email = user.Email;
                }

                await _context.SaveChangesAsync();

                return Ok (updatedUser);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
            
        }
    }
}