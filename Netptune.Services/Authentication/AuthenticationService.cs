using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Flurl;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Models.Messaging;

namespace Netptune.Services.Authentication
{
    public class NetptuneAuthService : INetptuneAuthService
    {
        private readonly SignInManager<AppUser> SignInManager;
        private readonly UserManager<AppUser> UserManager;
        private readonly IEmailService Email;

        protected readonly string Issuer;
        protected readonly string SecurityKey;
        protected readonly string ExpireDays;
        protected readonly string Origin;

        public NetptuneAuthService(
            IConfiguration configuration,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailService email
            )
        {
            SignInManager = signInManager;
            UserManager = userManager;
            Email = email;

            Issuer = configuration["Tokens:Issuer"];
            SecurityKey = configuration["Tokens:SecurityKey"];
            ExpireDays = configuration["Tokens:ExpireDays"];
            Origin = configuration["Origin"];
        }

        public async Task<LoginResult> LogIn(TokenRequest model)
        {
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, true, false);

            if (!result.Succeeded) return LoginResult.Failed("Username or password is incorrect");

            var appUser = await UserManager.FindByEmailAsync(model.Email);

            appUser.LastLoginTime = DateTime.UtcNow;

            await UserManager.UpdateAsync(appUser);

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

            var result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                if (result.Errors is null)
                {
                    return RegisterResult.Failed("Registration failed.");
                }

                return RegisterResult.Failed(string.Join(", ", result.Errors));
            }

            var appUser = await UserManager.FindByEmailAsync(model.Email);

            await SignInManager.SignInAsync(appUser, false);

            appUser.RegistrationDate = DateTime.UtcNow;
            appUser.LastLoginTime = DateTime.UtcNow;

            await UserManager.UpdateAsync(appUser);

            await SendWelcomeEmail(appUser);

            return RegisterResult.Success(GenerateToken(appUser));
        }

        public async Task<RegisterResult> ConfirmEmail(string userId, string code)
        {
            var user = await UserManager.FindByIdAsync(userId);

            if (user is null) return null;

            return await ConfirmEmail(user, code);
        }

        public async Task<RegisterResult> ConfirmEmail(AppUser user, string code)
        {
            var result = await UserManager.ConfirmEmailAsync(user, code);

            if (!result.Succeeded) return null;

            return RegisterResult.Success(GenerateToken(user));
        }

        private async Task SendWelcomeEmail(AppUser appUser)
        {
            var confirmEmailCode = await UserManager.GenerateEmailConfirmationTokenAsync(appUser);

            var callbackUrl = Origin
                .AppendPathSegments("app", "auth", "confirm")
                .SetQueryParam("userId", appUser.Id, true)
                .SetQueryParam("code", Uri.EscapeDataString(confirmEmailCode), true);

            var rawTextContent = $"Thanks for registering with Netptune. Confirm your email address with the following link. {callbackUrl}";

            await Email.Send(new SendEmailModel
            {
                ToDisplayName = $"{appUser.Firstname} {appUser.Lastname}",
                Subject = "Welcome To Netptune",
                RawTextContent = rawTextContent,
                ToAddress = appUser.Email,
                Action = "Confirm Email",
                Link = callbackUrl,
                PreHeader = "Thanks for signing up",
                Name = appUser.Firstname,
                Message = "Thanks for registering with Netptune. \n\n Confirm your email address with the following link."
            });
        }

        private DateTime GetExpireDays()
        {
            return DateTime.Now.AddDays(Convert.ToDouble(ExpireDays));
        }

        private AuthenticationTicket GenerateToken(AppUser appUser)
        {
            var expireDays = GetExpireDays();

            return new AuthenticationTicket
            {
                Token = GenerateJwtToken(appUser, expireDays),
                UserId = appUser.Id,
                EmailAddress = appUser.Email,
                DisplayName = appUser.GetDisplayName(),
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                Issuer,
                Issuer,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
