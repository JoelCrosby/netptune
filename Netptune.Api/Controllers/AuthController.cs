using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Netptune.Entities.Authentication;
using Netptune.Entities.Contexts;
using Netptune.Entities.Entites;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public AuthController(
            IConfiguration configuration,
            DataContext context,
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> Login([FromBody] TokenRequest model)
        {
            if (model.Username.IndexOf('@') > -1)
            {
                var user = await _userManager.FindByEmailAsync(model.Username.ToUpper());
                if (user == null)
                {
                    return BadRequest($"User with the following email address  '{model.Username}'  does not exist");
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

                var expireDays = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Tokens:ExpireDays"]));

                return Ok(new AuthenticationTicket
                {
                    Token = GenerateJwtToken(appUser, expireDays),
                    UserId = appUser.Id,
                    Username = model.Username,
                    Emailaddress = appUser.Email,
                    DisplayName = GetUserDisplayName(appUser),
                    Issued = DateTime.Now,
                    Expires = expireDays
                });
            }

            return BadRequest("Username or Password is incorrect");
        }

        private string GetUserDisplayName(AppUser user)
        {
            if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName))
            {
                return user.Email;
            }

            return $"{user.FirstName} {user.LastName}";
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> Register([FromBody] TokenRequest model)
        {
            var appUser = new AppUser
            {
                UserName = model.Username,
                Email = model.Username
            };
            var result = await _userManager.CreateAsync(appUser, model.Password);

            if (result.Succeeded)
            {
                if (_context.AppUsers.SingleOrDefault(x => x.Id == appUser.Id) is AppUser newUser)
                {
                    newUser.RegistrationDate = DateTime.UtcNow;
                    _context.SaveChanges();
                }

                await _signInManager.SignInAsync(appUser, false);

                var expireDays = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Tokens:ExpireDays"]));

                return Ok(new AuthenticationTicket
                {
                    Token = GenerateJwtToken(appUser, expireDays),
                    UserId = appUser.Id,
                    Username = model.Username,
                    Emailaddress = appUser.Email,
                    DisplayName = GetUserDisplayName(appUser),
                    Issued = DateTime.Now,
                    Expires = expireDays
                });
            }

            if (result.Errors is var errors)
            {
                return BadRequest(errors);
            }

            return BadRequest("Registration failed.");
        }

        private object GenerateJwtToken(AppUser user, DateTime expires)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:SecurityKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Tokens:Issuer"],
                audience: _configuration["Tokens:Issuer"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}