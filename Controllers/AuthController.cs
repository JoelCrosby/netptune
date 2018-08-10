using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataPlane.Data;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace DataPlane.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ProjectsContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration, ProjectsContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public class TokenRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult RequestToken([FromBody] TokenRequest request)
        {
            var user = _context.Users.Where(x => x.EmailAddress == request.Username || x.Username == request.Username).SingleOrDefault();

            if (user == null)
            {
                return BadRequest("Username does not exist!");
            }

            var password = _context.Passwords.Where(x => x.Owner.Id == user.Id).SingleOrDefault();
            var isCorrectPassword = Util.Cryptography.GetPasswordHash(request.Password, password.Salt).Hash == password.Hash;

            if (isCorrectPassword)
            {
                var claims = new[]
                {
                     new Claim(ClaimTypes.Name, request.Username)
                 };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["SecurityKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "DataPlane.com",
                    audience: "DataPlane.com",
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    username = user.Username,
                    emailaddress = user.EmailAddress,
                    displayName = user.DisplayName,
                    issued = DateTime.Now,
                    expires = DateTime.Now.AddMinutes(30)
                });
            }

            return BadRequest("Password incorrect.");
        }
    }
}
