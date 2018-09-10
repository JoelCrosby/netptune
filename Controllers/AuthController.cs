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
    public class AuthController : ControllerBase
    {
        private readonly ProjectsContext _context;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public AuthController(
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

        public class TokenRequest
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] TokenRequest model)
        {

            if (model.Username.IndexOf('@') > -1)
            {
                var user =  await _userManager.FindByEmailAsync(model.Username.ToUpper());
                if (user == null)
                {
                    return BadRequest("Invalid login attempt.");
                }
                else
                {
                    model.Username = user.UserName;
                }
            }

            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, true, false);

            if (result.Succeeded)
            {
                var appUser = _userManager.Users.SingleOrDefault(r => r.UserName == model.Username);

               if (_context.AppUsers.SingleOrDefault(x => x.Id == appUser.Id) is AppUser user)
               {
                   user.LastLoginTime = DateTime.UtcNow;
                   _context.SaveChanges();
               }
                

                return Ok(new
                {
                    token = GenerateJwtToken(model.Username, appUser),
                    userId = appUser.Id,
                    username = model.Username,
                    emailaddress = appUser.Email,
                    displayName = appUser.UserName,
                    issued = DateTime.Now,
                    expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]))
                });

            }

            return BadRequest("Username or Password is incorrect");
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] TokenRequest model)
        {
            var user = new AppUser
            {
                UserName = model.Username,
                Email = model.Email
            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {

                if (_context.AppUsers.SingleOrDefault(x => x.Id == user.Id) is AppUser newUser)
                {
                   newUser.RegistrationDate = DateTime.UtcNow;
                   _context.SaveChanges();
                }

                await _signInManager.SignInAsync(user, false);

                return Ok(new
                {
                    token = GenerateJwtToken(model.Username, user),
                    username = model.Username,
                    emailaddress = user.Email,
                    displayName = user.UserName,
                    issued = DateTime.Now,
                    expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]))
                });
            }

            if (result.Errors is var errors)
            {
                return BadRequest(errors);
            }

            return BadRequest("Registration failed.");
        }

        private object GenerateJwtToken(string email, AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["SecurityKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtIssuer"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
