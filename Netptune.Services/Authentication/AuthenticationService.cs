using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Models.Authentication;
using Netptune.Core.UnitOfWork;
using Netptune.Models;

namespace Netptune.Services.Authentication
{
    public class NetptuneAuthService : INetptuneAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly INetptuneUnitOfWork _unitOfWork;

        private readonly string _issuer;
        private readonly string _securityKey;
        private readonly string _expireDays;

        public NetptuneAuthService(
            IConfiguration configuration,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            INetptuneUnitOfWork unitOfWork
            )
        {
            _configuration = configuration;
            _signInManager = signInManager;
            _userManager = userManager;
            _unitOfWork = unitOfWork;

            _issuer = _configuration["Tokens:Issuer"];
            _securityKey = _configuration["Tokens:SecurityKey"];
            _expireDays = _configuration["Tokens:ExpireDays"];
        }

        public async Task<LoginResult> LogIn(TokenRequest model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, false);

            if (!result.Succeeded) return LoginResult.Failed("Username or password is incorrect");

            var appUser = await _userManager.FindByEmailAsync(model.Email);

            appUser.LastLoginTime = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();

            return LoginResult.Success(GenerateToken(appUser));
        }

        public async Task<RegisterResult> Register(RegisterRequest model)
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
                if (result.Errors is null)
                {
                    return RegisterResult.Failed("Registration failed.");
                }

                return RegisterResult.Failed(string.Join(", ", result.Errors));
            }

            var appUser = await _userManager.FindByEmailAsync(model.Email);

            await _signInManager.SignInAsync(appUser, false);

            appUser.RegistrationDate = DateTime.UtcNow;
            appUser.LastLoginTime = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();

            return RegisterResult.Success(GenerateToken(appUser));
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
                EmailAddress = appUser.Email,
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

        private static string GetUserDisplayName(AppUser user)
        {
            if (string.IsNullOrEmpty(user.Firstname) || string.IsNullOrEmpty(user.Lastname))
            {
                return user.Email;
            }

            return $"{user.Firstname} {user.Lastname}";
        }
    }
}
