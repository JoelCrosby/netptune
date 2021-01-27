using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Flurl;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Authentication
{
    public class NetptuneAuthService : INetptuneAuthService
    {
        private readonly SignInManager<AppUser> SignInManager;
        private readonly UserManager<AppUser> UserManager;
        private readonly IEmailService Email;
        private readonly IHttpContextAccessor ContextAccessor;
        private readonly IInviteCache InviteCache;
        private readonly IIdentityService Identity;
        private readonly INetptuneUnitOfWork UnitOfWork;

        protected readonly string Issuer;
        protected readonly string SecurityKey;
        protected readonly string ExpireDays;
        protected readonly string Origin;

        public NetptuneAuthService(
            IConfiguration configuration,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailService email,
            IHttpContextAccessor contextAccessor,
            IInviteCache inviteCache,
            IIdentityService identity,
            INetptuneUnitOfWork unitOfWork
            )
        {
            SignInManager = signInManager;
            UserManager = userManager;
            Email = email;
            ContextAccessor = contextAccessor;
            InviteCache = inviteCache;
            Identity = identity;
            UnitOfWork = unitOfWork;

            Issuer = configuration["Tokens:Issuer"];
            ExpireDays = configuration["Tokens:ExpireDays"];
            Origin = configuration["Origin"];

            SecurityKey = Environment.GetEnvironmentVariable("NETPTUNE_SIGNING_KEY");
        }

        public async Task<LoginResult> LogIn(TokenRequest model)
        {
            var appUser = await UserManager.FindByEmailAsync(model.Email);

            if (appUser is null)
            {
                return LoginResult.Failed("Username or password is incorrect");
            }

            var result = await SignInManager.CheckPasswordSignInAsync(appUser, model.Password, false);

            if (!result.Succeeded) return LoginResult.Failed("Username or password is incorrect");

            appUser.LastLoginTime = DateTime.UtcNow;

            await UserManager.UpdateAsync(appUser);

            return LoginResult.Success(GenerateToken(appUser));
        }

        public async Task<LoginResult> LogInViaProvider()
        {
            var email = await Identity.GetCurrentUserEmail();
            var user = await UserManager.FindByEmailAsync(email);

            if (user is null)
            {
                var registerRequest = new RegisterRequest
                {
                    Email = email,
                    Firstname = Identity.GetUserName().Split(" ").FirstOrDefault(),
                    Lastname = Identity.GetUserName().Split(" ").LastOrDefault(),
                    PictureUrl = Identity.GetPictureUrl(),
                    Password = null,
                    AuthenticationProvider = AuthenticationProvider.GitHub,
                };

                await Register(registerRequest);
            }

            user = await UserManager.FindByEmailAsync(email);
            user.LastLoginTime = DateTime.UtcNow;

            await UserManager.UpdateAsync(user);

            return LoginResult.Success(GenerateToken(user));
        }

        public async Task<RegisterResult> Register(RegisterRequest model)
        {
            async Task<WorkspaceInvite> GetWorkspaceInvite()
            {
                if (model.InviteCode is null) return null;

                return await ValidateInviteCode(model.InviteCode);
            }

            var invite = await GetWorkspaceInvite();

            if (model.InviteCode is {} && invite is null)
            {
                return RegisterResult.Failed("Invite code is invalid/expired.");
            }

            var user = new AppUser
            {
                Email = model.Email,
                UserName = model.Email,
                Firstname = model.Firstname,
                Lastname = model.Lastname,
                PictureUrl = model.PictureUrl,
            };

            var result = model.AuthenticationProvider switch
            {
                AuthenticationProvider.GitHub => await UserManager.CreateAsync(user),
                _ => await UserManager.CreateAsync(user, model.Password),
            };

            if (invite is {})
            {
                await UnitOfWork.WorkspaceUsers.AddAsync(new WorkspaceAppUser
                {
                    UserId = user.Id,
                    WorkspaceId = invite.WorkspaceId,
                });

                await UnitOfWork.CompleteAsync();

                InviteCache.Remove(model.InviteCode);
            }

            if (!result.Succeeded)
            {
                if (result.Errors is null)
                {
                    return RegisterResult.Failed("Registration failed.");
                }

                return RegisterResult.Failed(string.Join(", ", result.Errors));
            }

            var appUser = await UserManager.FindByEmailAsync(model.Email);

            await LogNewlyRegisteredUserIn(appUser);
            await SendWelcomeEmail(appUser);

            return RegisterResult.Success(GenerateToken(appUser));
        }

        public async Task<RegisterResult> ConfirmEmail(string userId, string code)
        {
            var user = await UserManager.FindByIdAsync(userId);

            if (user is null) return RegisterResult.Failed();

            return await ConfirmEmail(user, code);
        }

        public async Task<RegisterResult> ConfirmEmail(AppUser user, string code)
        {
            var result = await UserManager.ConfirmEmailAsync(user, code);

            if (!result.Succeeded) return RegisterResult.Failed();

            return RegisterResult.Success(GenerateToken(user));
        }

        public async Task<ClientResponse> RequestPasswordReset(RequestPasswordResetRequest request)
        {
            var user = await UserManager.FindByEmailAsync(request.Email);

            if (user is null) return ClientResponse.Failed();

            var resetCode = await UserManager.GeneratePasswordResetTokenAsync(user);

            var callbackUrl = Origin
                .AppendPathSegments("app", "auth", "reset-password")
                .SetQueryParam("userId", user.Id, true)
                .SetQueryParam("code", Uri.EscapeDataString(resetCode), true);

            var rawTextContent = $"Here is the password reset link that was requested for your account. {callbackUrl}";

            await Email.Send(new SendEmailModel
            {
                SendTo = new SendTo
                {
                    Address = user.Email,
                    DisplayName = $"{user.Firstname} {user.Lastname}",
                },
                Reason = "password reset",
                Subject = "Reset Password",
                RawTextContent = rawTextContent,
                Action = "Reset my password",
                Link = callbackUrl,
                PreHeader = "Reset Password",
                Name = user.Firstname,
                Message = "Click the following link to reset your password."
            });

            return ClientResponse.Success();
        }

        public async Task<LoginResult> ResetPassword(ResetPasswordRequest request)
        {
            var user = await UserManager.FindByIdAsync(request.UserId);

            if (user is null) return LoginResult.Failed("Password Reset Failed, userId or code was invalid");

            var result = await UserManager.ResetPasswordAsync(user, request.Code, request.Password);

            if (!result.Succeeded) return LoginResult.Failed("Password Reset Failed, userId or code was invalid");

            await LogUserIn(user);

            return LoginResult.Success(GenerateToken(user));
        }

        public async Task<ClientResponse> ChangePassword(ChangePasswordRequest request)
        {
            var user = await UserManager.FindByIdAsync(request.UserId);

            if (user is null) return ClientResponse.Failed();

            var result = await UserManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded) return ClientResponse.Failed();

            await LogUserIn(user);

            return ClientResponse.Success("Password Changed");
        }

        public async Task<CurrentUserResponse> CurrentUser()
        {
            var principle = ContextAccessor.HttpContext?.User;
            var user = await UserManager.GetUserAsync(principle);

            if (user is null) return null;

            return new CurrentUserResponse
            {
                DisplayName = user.DisplayName,
                EmailAddress = user.Email,
                PictureUrl = user.PictureUrl,
                UserId = user.Id,
            };
        }

        private async Task SendWelcomeEmail(AppUser appUser)
        {
            var confirmEmailCode = await UserManager.GenerateEmailConfirmationTokenAsync(appUser);

            var callbackUrl = Origin
                .AppendPathSegments("app", "auth", "confirm")
                .SetQueryParam("userId", appUser.Id, true)
                .SetQueryParam("code", Uri.EscapeDataString(confirmEmailCode), true);

            var rawTextContent = $"Thanks for registering with Netptune. Please confirm your email address with the following link. {callbackUrl}";

            await Email.Send(new SendEmailModel
            {
                SendTo = new SendTo
                {
                    Address = appUser.Email,
                    DisplayName = $"{appUser.Firstname} {appUser.Lastname}",
                },
                Reason = "email confirmation",
                Subject = "Welcome To Netptune",
                RawTextContent = rawTextContent,
                Action = "Confirm Email",
                Link = callbackUrl,
                PreHeader = "Thanks for signing up",
                Name = appUser.Firstname,
                Message = "Thanks for registering with Netptune. \n\n Please confirm your email address with the following link."
            });
        }

        private async Task LogNewlyRegisteredUserIn(AppUser appUser)
        {
            await SignInManager.SignInAsync(appUser, false);

            appUser.RegistrationDate = DateTime.UtcNow;
            appUser.LastLoginTime = DateTime.UtcNow;

            await UserManager.UpdateAsync(appUser);
        }

        private async Task LogUserIn(AppUser appUser)
        {
            await SignInManager.SignInAsync(appUser, false);

            appUser.LastLoginTime = DateTime.UtcNow;

            await UserManager.UpdateAsync(appUser);
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
                DisplayName = appUser.DisplayName,
                Issued = DateTime.Now,
                Expires = expireDays,
                PictureUrl = appUser.PictureUrl,
            };
        }

        private string GenerateJwtToken(AppUser user, DateTime expires)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
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

        public Task<WorkspaceInvite> ValidateInviteCode(string code)
        {
            return InviteCache.Get(code);
        }
    }
}
