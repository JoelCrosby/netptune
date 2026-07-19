using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Flurl;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.Entities;
using Netptune.Core.Extensions;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

using WorkspaceAppUser = Netptune.Core.Relationships.WorkspaceAppUser;

namespace Netptune.Identity.Authentication;

public class NetptuneAuthService : INetptuneAuthService
{
    private readonly SignInManager<AppUser> SignInManager;
    private readonly UserManager<AppUser> UserManager;
    private readonly IEmailService Email;
    private readonly IHttpContextAccessor ContextAccessor;
    private readonly IIdentityService Identity;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IWorkspacePermissionCache WorkspacePermissionCache;
    private readonly ICacheProvider Cache;

    private readonly string Issuer;
    private readonly string SecurityKey;
    private readonly string ExpireDays;
    private readonly string Origin;

    public NetptuneAuthService(
        IConfiguration configuration,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailService email,
        IHttpContextAccessor contextAccessor,
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork,
        IWorkspacePermissionCache workspacePermissionCache,
        ICacheProvider cache)
    {
        SignInManager = signInManager;
        UserManager = userManager;
        Email = email;
        ContextAccessor = contextAccessor;
        Identity = identity;
        UnitOfWork = unitOfWork;
        WorkspacePermissionCache = workspacePermissionCache;
        Cache = cache;

        Issuer = configuration.GetRequiredValue("Tokens:Issuer");
        ExpireDays = configuration.GetRequiredValue("Tokens:ExpireDays");
        Origin = configuration.GetRequiredValue("Origin");

        SecurityKey = Environment.GetEnvironmentVariable("NETPTUNE_SIGNING_KEY")!;
    }

    public async Task<LoginResult> LogIn(TokenRequest model)
    {
        var appUser = await UserManager.FindByEmailAsync(model.Email);

        if (model.Password is null || !IsInteractiveUser(appUser))
        {
            return LoginResult.Failed("Username or password is incorrect");
        }

        var result = await SignInManager.CheckPasswordSignInAsync(appUser, model.Password, false);

        if (!result.Succeeded) return LoginResult.Failed("Username or password is incorrect");

        appUser.LastLoginTime = DateTime.UtcNow;

        await UserManager.UpdateAsync(appUser);

        return LoginResult.Success(await GenerateTicket(appUser));
    }

    public async Task<LoginResult> Refresh(RefreshTokenRequest request)
    {
        var hasedToken = HashToken(request.RefreshToken);
        var existing = await UnitOfWork.RefreshTokens.GetByTokenAsync(hasedToken);

        if (existing is null || !existing.IsActive)
        {
            return LoginResult.Failed("Invalid or expired refresh token");
        }

        var appUser = await UserManager.FindByIdAsync(existing.UserId);

        if (!IsInteractiveUser(appUser))
        {
            return LoginResult.Failed("Invalid or expired refresh token");
        }

        await UnitOfWork.RefreshTokens.RevokeAsync(hasedToken);
        await UnitOfWork.RefreshTokens.RemoveExpiredAsync(appUser.Id);

        var ticket = await GenerateTicket(appUser);

        await UnitOfWork.CompleteAsync();

        return LoginResult.Success(ticket);
    }

    public async Task<LoginResult> LogInViaProvider(string providerScheme)
    {
        var email = Identity.GetCurrentUserEmail();
        var providerKey = Identity.GetProviderKey();

        var user = await UserManager.FindByLoginAsync(providerScheme, providerKey);

        if (user is null)
        {
            var existingByEmail = await UserManager.FindByEmailAsync(email);

            if (existingByEmail is not null)
            {
                if (!IsInteractiveUser(existingByEmail))
                {
                    return LoginResult.Failed("External login failed.");
                }

                return await CreateExternalLoginLinkResult(
                    existingByEmail,
                    providerScheme,
                    providerKey,
                    email);
            }
            else
            {
                var name = Identity.GetUserName();
                var nameParts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                var registerRequest = new RegisterRequest
                {
                    Email = email,
                    Firstname = nameParts.ElementAtOrDefault(0) ?? string.Empty,
                    Lastname = nameParts.ElementAtOrDefault(1) ?? string.Empty,
                    PictureUrl = Identity.GetPictureUrl(),
                    Password = null,
                    OAuthProvider = providerScheme,
                    OAuthProviderKey = providerKey,
                };

                await Register(registerRequest);

                user = await UserManager.FindByLoginAsync(providerScheme, providerKey);
            }
        }

        if (!IsInteractiveUser(user))
        {
            return LoginResult.Failed("External login failed.");
        }

        user.LastLoginTime = DateTime.UtcNow;

        await UserManager.UpdateAsync(user);

        var ticket = await GenerateTicket(user);

        return LoginResult.Success(ticket);
    }

    public async Task<LoginResult> LinkProvider(LinkProviderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return LoginResult.Failed("External login link is invalid or expired.");
        }

        var cacheKey = GetPendingExternalLoginCacheKey(request.Token);
        var pending = await Cache.GetValueAsync<PendingExternalLogin>(cacheKey);

        if (pending is null)
        {
            return LoginResult.Failed("External login link is invalid or expired.");
        }

        var currentUserId = Identity.GetCurrentUserId();

        if (!string.Equals(currentUserId, pending.ExistingUserId, StringComparison.Ordinal))
        {
            return LoginResult.Failed("External login link does not belong to the signed-in account.");
        }

        var user = await UserManager.FindByIdAsync(pending.ExistingUserId);

        if (!IsInteractiveUser(user))
        {
            return LoginResult.Failed("External login link is invalid or expired.");
        }

        var loginInfo = new UserLoginInfo(pending.Provider, pending.ProviderKey, pending.Provider);
        var linkResult = await UserManager.AddLoginAsync(user, loginInfo);

        if (!linkResult.Succeeded)
        {
            return LoginResult.Failed(string.Join(", ", linkResult.Errors.Select(error => error.Description)));
        }

        await Cache.RemoveAsync(cacheKey);

        user.LastLoginTime = DateTime.UtcNow;

        await UserManager.UpdateAsync(user);

        var ticket = await GenerateTicket(user);

        return LoginResult.Success(ticket);
    }

    private async Task<LoginResult> CreateExternalLoginLinkResult(
        AppUser existingUser,
        string providerScheme,
        string providerKey,
        string email)
    {
        var token = GenerateSecureToken();
        var cacheKey = GetPendingExternalLoginCacheKey(token);

        await Cache.SetAsync(cacheKey, new PendingExternalLogin
        {
            ExistingUserId = existingUser.Id,
            Provider = providerScheme,
            ProviderKey = providerKey,
            Email = email,
            DisplayName = Identity.GetUserName(),
            PictureUrl = Identity.GetPictureUrl(),
            Created = DateTime.UtcNow,
        }, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        });

        return LoginResult.LinkRequired(new ExternalLoginLink
        {
            Token = token,
            Provider = providerScheme,
            Email = email,
        });
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GetPendingExternalLoginCacheKey(string token)
    {
        return $"auth:external-link:{HashToken(token)}";
    }

    public async Task<RegisterResult> Register(RegisterRequest model)
    {
        async Task<WorkspaceInvite?> GetWorkspaceInvite()
        {
            if (model.InviteCode is null) return null;

            return await ValidateInviteCode(model.InviteCode);
        }

        var invite = await GetWorkspaceInvite();

        if (model.InviteCode is not null && invite is null)
        {
            return RegisterResult.Failed("Invite code is invalid/expired.");
        }

        if (model.Password is null && model.OAuthProvider is null)
        {
            return RegisterResult.Failed("Invalid request.");
        }

        var existingUser = await UserManager.FindByEmailAsync(model.Email);

        if (existingUser is not null)
        {
            return RegisterResult.Failed("User with email already exists");
        }

        var user = new AppUser
        {
            Email = model.Email,
            UserName = model.Email,
            Firstname = model.Firstname,
            Lastname = model.Lastname,
            PictureUrl = model.PictureUrl,
        };

        var result = model.OAuthProvider is not null
            ? await UserManager.CreateAsync(user)
            : await UserManager.CreateAsync(user, model.Password!);

        if (invite is not null)
        {
            var permissions = WorkspaceRolePermissions
                .GetDefaultPermissions(WorkspaceRole.Member)
                .ToList();

            await UnitOfWork.WorkspaceUsers.AddAsync(new WorkspaceAppUser
            {
                UserId = user.Id,
                WorkspaceId = invite.WorkspaceId,
                Role = WorkspaceRole.Member,
                Permissions = permissions,
            }, CancellationToken.None);

            await UnitOfWork.WorkspaceInvites.Accept(model.InviteCode!, CancellationToken.None);

            await UnitOfWork.CompleteAsync();
        }

        if (!result.Succeeded)
        {
            return RegisterResult.Failed(string.Join(", ", result.Errors));
        }

        var appUser = await UserManager.FindByEmailAsync(model.Email);

        if (appUser is null)
        {
            throw new InvalidOperationException("Invalid request.");
        }

        if (model.OAuthProvider is not null && model.OAuthProviderKey is not null)
        {
            var loginInfo = new UserLoginInfo(model.OAuthProvider, model.OAuthProviderKey, model.OAuthProvider);
            await UserManager.AddLoginAsync(appUser, loginInfo);
        }

        await LogNewlyRegisteredUserIn(appUser);
        await SendWelcomeEmail(appUser);

        var ticket = await GenerateTicket(appUser);

        return RegisterResult.Success(ticket);
    }

    public async Task<RegisterResult> ConfirmEmail(string userId, string code)
    {
        var user = await UserManager.FindByIdAsync(userId);

        if (!IsInteractiveUser(user)) return RegisterResult.Failed();

        return await ConfirmEmail(user, code);
    }

    public async Task<RegisterResult> ConfirmEmail(AppUser user, string code)
    {
        if (!IsInteractiveUser(user)) return RegisterResult.Failed();

        var result = await UserManager.ConfirmEmailAsync(user, code);

        if (!result.Succeeded) return RegisterResult.Failed();

        var ticket = await GenerateTicket(user);

        return RegisterResult.Success(ticket);
    }

    public async Task<ClientResponse> RequestPasswordReset(RequestPasswordResetRequest request)
    {
        var user = await UserManager.FindByEmailAsync(request.Email);

        if (!IsInteractiveUser(user)) return ClientResponse.Failed();

        var resetCode = await UserManager.GeneratePasswordResetTokenAsync(user);

        var callbackUrl = Origin
            .AppendPathSegments("auth", "reset-password")
            .SetQueryParam("userId", user.Id, true)
            .SetQueryParam("code", Uri.EscapeDataString(resetCode), true);

        var rawTextContent = $"Here is the password reset link that was requested for your account. {callbackUrl}";

        await Email.Send(new SendEmailModel
        {
            SendTo = new SendTo
            {
                Address = user.Email!,
                DisplayName = $"{user.Firstname} {user.Lastname}",
            },
            Reason = "password reset",
            Subject = "Reset Password",
            RawTextContent = rawTextContent,
            Action = "Reset my password",
            Link = callbackUrl,
            PreHeader = "Reset Password",
            Name = user.Firstname,
            Message = "Click the following link to reset your password.",
        });

        return ClientResponse.Success;
    }

    public async Task<LoginResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await UserManager.FindByIdAsync(request.UserId);

        if (!IsInteractiveUser(user)) return LoginResult.Failed("Password Reset Failed, userId or code was invalid");

        var result = await UserManager.ResetPasswordAsync(user, request.Code, request.Password);

        if (!result.Succeeded) return LoginResult.Failed("Password Reset Failed, userId or code was invalid");

        await LogUserIn(user);

        var ticket = await GenerateTicket(user);

        return LoginResult.Success(ticket);
    }

    public async Task<ClientResponse> ChangePassword(ChangePasswordRequest request)
    {
        var user = await UserManager.FindByIdAsync(request.UserId);

        if (!IsInteractiveUser(user)) return ClientResponse.Failed();

        var result = await UserManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded) return ClientResponse.Failed();

        await LogUserIn(user);

        return ClientResponse.Success;
    }

    public async Task<CurrentUserResponse?> CurrentUser()
    {
        var principle = ContextAccessor.HttpContext?.User;

        if (principle is null) return null;

        var user = await UserManager.GetUserAsync(principle);

        if (user is null) return null;

        var workspace = Identity.TryGetWorkspaceKey();
        var workspaceUser = await WorkspacePermissionCache.GetUserPermissions(user.Id, workspace);

        if (workspaceUser is null) return null;

        return new CurrentUserResponse
        {
            DisplayName = user.DisplayName,
            EmailAddress = user.Email!,
            PictureUrl = user.PictureUrl,
            UserId = user.Id,
            UserPermissions = workspaceUser,
        };
    }

    private async Task SendWelcomeEmail(AppUser appUser)
    {
        var confirmEmailCode = await UserManager.GenerateEmailConfirmationTokenAsync(appUser);

        var callbackUrl = Origin
            .AppendPathSegments("auth", "confirm")
            .SetQueryParam("userId", appUser.Id, true)
            .SetQueryParam("code", Uri.EscapeDataString(confirmEmailCode), true);

        var rawTextContent = $"Thanks for registering with Netptune. Please confirm your email address with the following link. {callbackUrl}";

        await Email.Send(new SendEmailModel
        {
            SendTo = new SendTo
            {
                Address = appUser.Email!,
                DisplayName = $"{appUser.Firstname} {appUser.Lastname}",
            },
            Reason = "email confirmation",
            Subject = "Welcome To Netptune",
            RawTextContent = rawTextContent,
            Action = "Confirm Email",
            Link = callbackUrl,
            PreHeader = "Thanks for signing up",
            Name = appUser.Firstname,
            Message = "Thanks for registering with Netptune. \n\n Please confirm your email address with the following link.",
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
        return DateTime.UtcNow.AddDays(Convert.ToDouble(ExpireDays));
    }

    private async Task<AuthenticationTicket> GenerateTicket(AppUser appUser)
    {
        if (!IsInteractiveUser(appUser))
        {
            throw new InvalidOperationException("Service accounts cannot receive interactive login tickets.");
        }

        var expireDays = GetExpireDays();
        var refreshToken = await CreateRefreshToken(appUser.Id);

        return new AuthenticationTicket
        {
            Token = GenerateJwtToken(appUser, expireDays),
            RefreshToken = refreshToken,
            UserId = appUser.Id,
            EmailAddress = appUser.Email!,
            DisplayName = appUser.DisplayName,
            Issued = DateTime.UtcNow,
            Expires = expireDays,
            PictureUrl = appUser.PictureUrl,
        };
    }

    private async Task<string> CreateRefreshToken(string userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashedToken = HashToken(token);

        var refreshToken = new RefreshToken
        {
            Token = hashedToken,
            UserId = userId,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(30),
        };

        await UnitOfWork.RefreshTokens.AddAsync(refreshToken);
        await UnitOfWork.CompleteAsync();

        return token;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private string GenerateJwtToken(AppUser user, DateTime expires)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(NetptuneClaims.ActorType, AppUserType.User.ToString()),
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

    public async Task<WorkspaceInvite?> ValidateInviteCode(string code)
    {
        var invite = await UnitOfWork.WorkspaceInvites.GetByCode(code);

        if (invite is null) return null;

        return new WorkspaceInvite
        {
            Email = invite.Email,
            WorkspaceId = invite.WorkspaceId,
            Code = invite.Code,
        };
    }

    public async Task<IList<string>> GetLoginProviders()
    {
        var user = await Identity.GetCurrentUser();
        var logins = await UserManager.GetLoginsAsync(user);

        return logins.Select(l => l.LoginProvider).ToList();
    }

    private static bool IsInteractiveUser([NotNullWhen(true)] AppUser? user)
    {
        return user?.UserType == AppUserType.User;
    }
}
