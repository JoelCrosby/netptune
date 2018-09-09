using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DataPlane.Entites;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using DataPlane.Models;

namespace DataPlane.Controllers
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

            var user = _context.Users.Where(x => x.Id == id);

            if (user == null) {
                return NotFound ();
            }

            return Ok (user);
        } 
    }
}