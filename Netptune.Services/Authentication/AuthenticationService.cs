using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Models;
using Netptune.Models;
using Netptune.Services.Authentication.Interfaces;
using Netptune.Services.Authentication.Models;

namespace Netptune.Services.Authentication
{
    public class NetptuneAuthService : INetptuneAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        private readonly string _issuer;
        private readonly string _securityKey;
        private readonly string _expireDays;

        public NetptuneAuthService(
            IConfiguration configuration,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager
            )
        {
            _configuration = configuration;
            _signInManager = signInManager;
            _userManager = userManager;

            _issuer = _configuration["Tokens:Issuer"];
            _securityKey = _configuration["Tokens:SecurityKey"];
            _expireDays = _configuration["Tokens:ExpireDays"];
        }

        public async Task<ServiceResult<AuthenticationTicket>> LogIn(TokenRequest model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, false);

            if (!result.Succeeded) return ServiceResult<AuthenticationTicket>.BadRequest("Username or Password is incorrect");

            var appUser = await _userManager.FindByEmailAsync(model.Email);

            return ServiceResult<AuthenticationTicket>.Ok(GenerateToken(appUser));
        }

        public async Task<ServiceResult<AuthenticationTicket>> Register(RegisterRequest model)
        {
            var user = new AppUser
            {
                Email = model.Email,
                UserName = model.Email,
                Firstname = model.Firstname,
                Lastname = model.Lastname
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                if (result.Errors is var errors)
                {
                    return ServiceResult<AuthenticationTicket>.BadRequest(errors.Join(", "));
                }

                return ServiceResult<AuthenticationTicket>.BadRequest("Registration failed.");
            }

            var appUser = await _userManager.FindByEmailAsync(model.Email);

            await _signInManager.SignInAsync(appUser, false);

            return ServiceResult<AuthenticationTicket>.Ok(GenerateToken(appUser));
        }

        private DateTime GetExpireDays()
        {
            return DateTime.Now.AddDays(Convert.ToDouble(_expireDays));
        }

        private AuthenticationTicket GenerateToken(AppUser appUser)
        {
            var expireDays = GetExpireDays();

            return new AuthenticationTicket
            {
                Token = GenerateJwtToken(appUser, expireDays),
                UserId = appUser.Id,
                Emailaddress = appUser.Email,
                DisplayName = GetUserDisplayName(appUser),
                Issued = DateTime.Now,
                Expires = expireDays
            };
        }

        private string GenerateJwtToken(AppUser user, DateTime expires)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_securityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _issuer,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GetUserDisplayName(AppUser user)
        {
            if (string.IsNullOrEmpty(user.Firstname) || string.IsNullOrEmpty(user.Lastname))
            {
                return user.Email;
            }

            return $"{user.Firstname} {user.Lastname}";
        }
    }
}
