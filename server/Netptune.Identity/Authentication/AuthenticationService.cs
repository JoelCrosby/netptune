using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using AspNet.Security.OAuth.GitHub;

using Flurl;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Exceptions;
using Netptune.Core.Extensions;
using Netptune.Core.Localization;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;

using WorkspaceAppUser = Netptune.Core.Relationships.WorkspaceAppUser;
using WorkspaceInviteEntity = Netptune.Core.Relationships.WorkspaceInvite;

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
    private readonly IWorkspaceUserCache WorkspaceUserCache;
    private readonly ICacheProvider Cache;
    private readonly IEventRecordWriter EventRecords;

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
        IWorkspaceUserCache workspaceUserCache,
        ICacheProvider cache,
        IEventRecordWriter eventRecords,
        IOptions<NetptuneAuthenticationOptions> authenticationOptions)
    {
        SignInManager = signInManager;
        UserManager = userManager;
        Email = email;
        ContextAccessor = contextAccessor;
        Identity = identity;
        UnitOfWork = unitOfWork;
        WorkspacePermissionCache = workspacePermissionCache;
        WorkspaceUserCache = workspaceUserCache;
        Cache = cache;
        EventRecords = eventRecords;

        Issuer = configuration.GetRequiredValue("Tokens:Issuer");
        ExpireDays = configuration.GetRequiredValue("Tokens:ExpireDays");
        Origin = configuration.GetRequiredValue("Origin");

        SecurityKey = authenticationOptions.Value.SigningKey;
    }

    public async Task<LoginResult> LogIn(TokenRequest model)
    {
        var appUser = await UserManager.FindByEmailAsync(model.Email);

        if (model.Password is null || !IsInteractiveUser(appUser))
        {
            await LogAuthenticationEvent(appUser, model.Email, "password", succeeded: false);

            return LoginResult.Failed("Username or password is incorrect");
        }

        var result = await SignInManager.CheckPasswordSignInAsync(appUser, model.Password, false);

        if (!result.Succeeded)
        {
            await LogAuthenticationEvent(appUser, model.Email, "password", succeeded: false);

            return LoginResult.Failed("Username or password is incorrect");
        }

        appUser.LastLoginTime = DateTime.UtcNow;

        await UserManager.UpdateAsync(appUser);

        var ticket = await GenerateTicket(appUser);

        await LogAuthenticationEvent(appUser, model.Email, "password", succeeded: true);

        return LoginResult.Success(ticket);
    }

    public async Task<LoginResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var hasedToken = HashToken(request.RefreshToken);
        var existing = await UnitOfWork.RefreshTokens.GetByTokenAsync(hasedToken, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            return LoginResult.Failed("Invalid or expired refresh token");
        }

        var appUser = await UserManager.FindByIdAsync(existing.UserId);

        if (!IsInteractiveUser(appUser))
        {
            return LoginResult.Failed("Invalid or expired refresh token");
        }

        var result = await UnitOfWork.Transaction(async () =>
        {
            var revoked = await UnitOfWork.RefreshTokens.TryRevokeAsync(hasedToken, cancellationToken);

            if (!revoked)
            {
                return LoginResult.Failed("Invalid or expired refresh token");
            }

            await UnitOfWork.RefreshTokens.RemoveExpiredAsync(appUser.Id, cancellationToken);

            var ticket = await GenerateTicket(appUser);

            await UnitOfWork.CompleteAsync(cancellationToken);

            return LoginResult.Success(ticket);
        });

        return result;
    }

    public async Task<LoginResult> LogInViaProvider(string providerScheme)
    {
        var providerKey = Identity.GetProviderKey();
        var email = Identity.TryGetCurrentUserEmail() ?? string.Empty;

        var user = await UserManager.FindByLoginAsync(providerScheme, providerKey);

        if (user is null)
        {
            // An unverified provider address proves nothing about who owns it, so it must neither
            // create an account nor offer to link onto the account that already uses it.
            var hasEmail = !string.IsNullOrWhiteSpace(email);
            var isEmailVerified = hasEmail && Identity.IsProviderEmailVerified();

            if (!isEmailVerified)
            {
                await LogAuthenticationEvent(null, email, providerScheme, succeeded: false);

                return LoginResult.Failed("External login failed.");
            }

            var existingByEmail = await UserManager.FindByEmailAsync(email);

            if (existingByEmail is not null)
            {
                if (!IsInteractiveUser(existingByEmail))
                {
                    await LogAuthenticationEvent(existingByEmail, email, providerScheme, succeeded: false);

                    return LoginResult.Failed("External login failed.");
                }

                return await CreateExternalLoginLinkResult(
                    existingByEmail,
                    providerScheme,
                    providerKey,
                    email);
            }

            user = await RegisterExternalUser(providerScheme, providerKey, email);
        }

        if (!IsInteractiveUser(user))
        {
            await LogAuthenticationEvent(user, email, providerScheme, succeeded: false);

            return LoginResult.Failed("External login failed.");
        }

        user.LastLoginTime = DateTime.UtcNow;

        await UserManager.UpdateAsync(user);
        await RecordSourceControlIdentity(user.Id, providerScheme, providerKey, Identity.GetProviderUsername());

        var ticket = await GenerateTicket(user);

        await LogAuthenticationEvent(user, email, providerScheme, succeeded: true);

        return LoginResult.Success(ticket);
    }

    private async Task RecordSourceControlIdentity(
        string userId,
        string providerScheme,
        string providerKey,
        string? providerUsername)
    {
        var isGitHub = providerScheme == GitHubAuthenticationDefaults.AuthenticationScheme;

        if (!isGitHub)
        {
            return;
        }

        await UnitOfWork.UserSourceControlIdentities.RecordVerified(
            userId,
            SourceControlProvider.GitHub,
            providerKey,
            providerUsername,
            DateTime.UtcNow);

        await UnitOfWork.CompleteAsync();
    }

    private async Task LogAuthenticationEvent(AppUser? user, string email, string method, bool succeeded)
    {
        var workspaceIds = user is null
            ? []
            : await UnitOfWork.WorkspaceUsers.GetWorkspaceIdsForUser(user.Id);

        if (workspaceIds.Count == 0)
        {
            var workspaceKey = Identity.TryGetWorkspaceKey();
            var workspaceId = workspaceKey is null
                ? null
                : await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

            if (workspaceId.HasValue)
            {
                workspaceIds.Add(workspaceId.Value);
            }
        }

        var eventKey = succeeded
            ? EventKeys.SecurityLoginSucceeded
            : EventKeys.SecurityLoginFailed;

        var actorUserId = succeeded ? user?.Id : null;
        var payload = new AuthenticationEventPayload
        {
            Method = method,
            Email = email,
        };

        if (workspaceIds.Count == 0)
        {
            var subjectId = user?.Id ?? email.Trim().ToLowerInvariant();

            await EventRecords.Append(new EventWriteRequest<AuthenticationEventPayload>
            {
                EventKey = eventKey,
                SubjectType = EventEntityTypes.From(EntityType.User),
                SubjectId = string.IsNullOrWhiteSpace(subjectId) ? "unknown" : subjectId,
                ActorUserId = actorUserId,
                ResolveActorFromIdentity = false,
                Payload = payload,
            });
        }
        else
        {
            foreach (var workspaceId in workspaceIds.Distinct())
            {
                await EventRecords.Append(new EventWriteRequest<AuthenticationEventPayload>
                {
                    WorkspaceId = workspaceId,
                    EventKey = eventKey,
                    SubjectType = EventEntityTypes.From(EntityType.Workspace),
                    SubjectId = workspaceId.ToString(),
                    ActorUserId = actorUserId,
                    ResolveActorFromIdentity = false,
                    Payload = payload,
                });
            }
        }

        await UnitOfWork.CompleteAsync();
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

        await RecordSourceControlIdentity(user.Id, pending.Provider, pending.ProviderKey, pending.ProviderUsername);

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
            ProviderUsername = Identity.GetProviderUsername(),
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
        if (string.IsNullOrEmpty(model.Password))
        {
            return RegisterResult.Failed(AppText.ErrorRegisterPasswordRequired);
        }

        var invite = model.InviteCode is null ? null : await UnitOfWork.WorkspaceInvites.GetByCode(model.InviteCode);
        var hasInviteCode = model.InviteCode is not null;
        var isInviteForEmail = invite is not null && IsSameEmail(invite.Email, model.Email);

        if (hasInviteCode && !isInviteForEmail)
        {
            return RegisterResult.Failed(AppText.ErrorWorkspaceInviteInvalid);
        }

        var creation = await CreateUser(new NewUser
        {
            Email = model.Email,
            Firstname = model.Firstname,
            Lastname = model.Lastname,
            Password = model.Password,
            // The invite code only ever went to the invited inbox, so redeeming it proves the address.
            IsEmailVerified = invite is not null,
            Invite = invite,
        });

        if (creation.User is null)
        {
            return RegisterResult.Failed(creation.Error);
        }

        var user = creation.User;

        await LogNewlyRegisteredUserIn(user);

        if (!user.EmailConfirmed)
        {
            await SendWelcomeEmail(user);
        }

        var ticket = await GenerateTicket(user);

        return RegisterResult.Success(ticket);
    }

    private async Task<AppUser?> RegisterExternalUser(string providerScheme, string providerKey, string email)
    {
        var name = Identity.GetUserName();
        var nameParts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var creation = await CreateUser(new NewUser
        {
            Email = email,
            Firstname = nameParts.ElementAtOrDefault(0) ?? string.Empty,
            Lastname = nameParts.ElementAtOrDefault(1) ?? string.Empty,
            PictureUrl = Identity.GetPictureUrl(),
            IsEmailVerified = true,
            Login = new UserLoginInfo(providerScheme, providerKey, providerScheme),
        });

        if (creation.User is null)
        {
            return null;
        }

        await LogNewlyRegisteredUserIn(creation.User);

        return creation.User;
    }

    private async Task<UserCreation> CreateUser(NewUser newUser)
    {
        var existingUser = await UserManager.FindByEmailAsync(newUser.Email);

        if (existingUser is not null)
        {
            return UserCreation.Failed(AppText.ErrorRegisterEmailTaken);
        }

        var user = new AppUser
        {
            Email = newUser.Email,
            UserName = newUser.Email,
            Firstname = newUser.Firstname,
            Lastname = newUser.Lastname,
            PictureUrl = newUser.PictureUrl,
            EmailConfirmed = newUser.IsEmailVerified,
        };

        UserCreation? creation = null;

        try
        {
            await UnitOfWork.Transaction(async () =>
            {
                var created = newUser.Password is null
                    ? await UserManager.CreateAsync(user)
                    : await UserManager.CreateAsync(user, newUser.Password);

                if (!created.Succeeded)
                {
                    creation = UserCreation.Failed(DescribeErrors(created));

                    return;
                }

                if (newUser.Login is not null)
                {
                    var linked = await UserManager.AddLoginAsync(user, newUser.Login);

                    if (!linked.Succeeded)
                    {
                        throw new RolledBackException(DescribeErrors(linked));
                    }
                }

                if (newUser.Invite is not null)
                {
                    var workspace = await JoinInvitedWorkspace(user, newUser.Invite);

                    if (workspace is null)
                    {
                        throw new RolledBackException(AppText.ErrorWorkspaceInviteInvalid);
                    }
                }

                creation = UserCreation.Success(user);
            });
        }
        catch (Exception exception) when (AsRolledBack(exception) is { } rolledBack)
        {
            return UserCreation.Failed(rolledBack.Message);
        }
        catch (UniqueConstraintException)
        {
            return UserCreation.Failed(AppText.ErrorRegisterEmailTaken);
        }

        return creation!;
    }

    private async Task<Workspace?> JoinInvitedWorkspace(AppUser user, WorkspaceInviteEntity invite)
    {
        var workspace = await UnitOfWork.Workspaces.GetAsync(invite.WorkspaceId, true);

        if (workspace is null || workspace.IsDeleted)
        {
            return null;
        }

        var accepted = await UnitOfWork.WorkspaceInvites.TryAccept(invite.Code);

        if (!accepted)
        {
            return null;
        }

        var isMember = await UnitOfWork.Users.IsUserInWorkspace(user.Id, invite.WorkspaceId);

        if (isMember)
        {
            return workspace;
        }

        var permissions = WorkspaceRolePermissions
            .GetDefaultPermissions(WorkspaceRole.Member)
            .ToList();

        await UnitOfWork.WorkspaceUsers.AddAsync(new WorkspaceAppUser
        {
            UserId = user.Id,
            WorkspaceId = invite.WorkspaceId,
            Role = WorkspaceRole.Member,
            Permissions = permissions,
        });

        await UnitOfWork.CompleteAsync();

        return workspace;
    }

    public async Task<ClientResponse<AcceptWorkspaceInviteResponse>> AcceptWorkspaceInvite(AcceptWorkspaceInviteRequest request)
    {
        var user = await UserManager.FindByIdAsync(Identity.GetCurrentUserId());
        var invite = await UnitOfWork.WorkspaceInvites.GetByCode(request.Code);

        if (!IsInteractiveUser(user) || invite is null)
        {
            return ClientResponse<AcceptWorkspaceInviteResponse>.Failed(AppText.ErrorWorkspaceInviteInvalid);
        }

        var isInviteForUser = IsSameEmail(invite.Email, user.Email);

        if (!isInviteForUser)
        {
            return ClientResponse<AcceptWorkspaceInviteResponse>.Failed(AppText.ErrorWorkspaceInviteForAnotherEmail);
        }

        Workspace? workspace;

        try
        {
            workspace = await UnitOfWork.Transaction(async () =>
            {
                var joined = await JoinInvitedWorkspace(user, invite);

                if (joined is null || user.EmailConfirmed)
                {
                    return joined;
                }

                user.EmailConfirmed = true;

                var updated = await UserManager.UpdateAsync(user);

                if (!updated.Succeeded)
                {
                    throw new RolledBackException(DescribeErrors(updated));
                }

                return joined;
            });
        }
        catch (Exception exception) when (AsRolledBack(exception) is not null)
        {
            workspace = null;
        }

        if (workspace is null)
        {
            return ClientResponse<AcceptWorkspaceInviteResponse>.Failed(AppText.ErrorWorkspaceInviteInvalid);
        }

        var cacheKey = new WorkspaceUserKey { UserId = user.Id, WorkspaceKey = workspace.Slug };

        WorkspaceUserCache.Remove(cacheKey);
        WorkspacePermissionCache.Remove(cacheKey);

        return new AcceptWorkspaceInviteResponse { WorkspaceSlug = workspace.Slug };
    }

    private static bool IsSameEmail(string inviteEmail, string? email)
    {
        if (email is null)
        {
            return false;
        }

        return inviteEmail.Trim().IdentityNormalize() == email.Trim().IdentityNormalize();
    }

    private static RolledBackException? AsRolledBack(Exception exception)
    {
        return exception as RolledBackException ?? exception.InnerException as RolledBackException;
    }

    private static string DescribeErrors(IdentityResult result)
    {
        return string.Join(" ", result.Errors.Select(error => error.Description));
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

        // A confirm link signs its holder in, so it must stop working once it has done its one job.
        if (user.EmailConfirmed)
        {
            return RegisterResult.Failed();
        }

        var result = await UserManager.ConfirmEmailAsync(user, code);

        if (!result.Succeeded) return RegisterResult.Failed();

        var ticket = await GenerateTicket(user);

        return RegisterResult.Success(ticket);
    }

    public async Task<ClientResponse> RequestPasswordReset(RequestPasswordResetRequest request)
    {
        var user = await UserManager.FindByEmailAsync(request.Email);

        // The same answer either way, so the form cannot be used to find out who has an account.
        if (!IsInteractiveUser(user)) return ClientResponse.Success;

        var resetCode = await UserManager.GeneratePasswordResetTokenAsync(user);

        var callbackUrl = Origin
            .AppendPathSegments("auth", "reset-password")
            .SetQueryParam("userId", user.Id, true)
            .SetQueryParam("code", Uri.EscapeDataString(resetCode), true);

        var sendTo = new SendTo
        {
            Address = user.Email!,
            DisplayName = $"{user.Firstname} {user.Lastname}",
        };
        var email = EmailMessages.PasswordReset(sendTo, user.Firstname, callbackUrl);

        await Email.Send(email);

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

    public async Task<ClientResponse> SetPassword(SetPasswordRequest request)
    {
        var user = await UserManager.FindByIdAsync(Identity.GetCurrentUserId());

        if (!IsInteractiveUser(user)) return ClientResponse.Failed();

        var hasPassword = await UserManager.HasPasswordAsync(user);

        // Adding a password is only for accounts created through an external
        // provider. Anyone with a password already must go through ChangePassword,
        // which proves knowledge of the current one.
        if (hasPassword)
        {
            return ClientResponse.Failed("This account already has a password.");
        }

        var result = await UserManager.AddPasswordAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return ClientResponse.Failed(string.Join(", ", result.Errors.Select(error => error.Description)));
        }

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

        var sendTo = new SendTo
        {
            Address = appUser.Email!,
            DisplayName = $"{appUser.Firstname} {appUser.Lastname}",
        };
        var email = EmailMessages.Welcome(sendTo, appUser.Firstname, callbackUrl);

        await Email.Send(email);
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

    public async Task<LoginMethods> GetLoginMethods()
    {
        // Loaded through the UserManager rather than the identity cache, whose copy of the
        // user can predate a password being set and would report HasPassword incorrectly.
        var user = await UserManager.FindByIdAsync(Identity.GetCurrentUserId());

        if (user is null) return new LoginMethods();

        var logins = await UserManager.GetLoginsAsync(user);
        var hasPassword = await UserManager.HasPasswordAsync(user);

        return new LoginMethods
        {
            Providers = logins.Select(l => l.LoginProvider).ToList(),
            HasPassword = hasPassword,
        };
    }

    private static bool IsInteractiveUser([NotNullWhen(true)] AppUser? user)
    {
        return user?.UserType == AppUserType.User;
    }

    private sealed record NewUser
    {
        public required string Email { get; init; }

        public required string Firstname { get; init; }

        public required string Lastname { get; init; }

        public string? Password { get; init; }

        public string? PictureUrl { get; init; }

        public bool IsEmailVerified { get; init; }

        public UserLoginInfo? Login { get; init; }

        public WorkspaceInviteEntity? Invite { get; init; }
    }

    private sealed record UserCreation
    {
        public AppUser? User { get; init; }

        public string? Error { get; init; }

        public static UserCreation Success(AppUser user) => new() { User = user };

        public static UserCreation Failed(string error) => new() { Error = error };
    }

    private sealed class RolledBackException(string message) : Exception(message);
}
