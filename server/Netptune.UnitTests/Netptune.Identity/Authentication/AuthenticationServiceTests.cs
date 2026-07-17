using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

using Netptune.Core.Authentication.Models;
using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Cache.Common;
using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Models;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Identity.Authentication;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

using RelationshipInvite = Netptune.Core.Relationships.WorkspaceInvite;
using WorkspaceAppUser = Netptune.Core.Relationships.WorkspaceAppUser;

namespace Netptune.UnitTests.Netptune.Identity.Authentication;

public class AuthenticationServiceTests
{
    private readonly NetptuneAuthService Service;

    private readonly UserManager<AppUser> UserManager;
    private readonly SignInManager<AppUser> SignInManager;
    private readonly IEmailService Email = Substitute.For<IEmailService>();
    private readonly IHttpContextAccessor ContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IConfiguration Configuration = Substitute.For<IConfiguration>();
    private readonly IWorkspacePermissionCache WorkspacePermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly ICacheProvider Cache = Substitute.For<ICacheProvider>();

    private const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256";

    public AuthenticationServiceTests()
    {
        Environment.SetEnvironmentVariable(
            "NETPTUNE_SIGNING_KEY",
            SigningKey);

        Configuration["Tokens:Issuer"].Returns("test-issuer");
        Configuration["Tokens:ExpireDays"].Returns("7");
        Configuration["Origin"].Returns("https://test.example.com");

        var userStore = Substitute.For<IUserStore<AppUser>>();

        UserManager = Substitute.For<UserManager<AppUser>>(
            userStore,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<AppUser>>();

        SignInManager = Substitute.For<SignInManager<AppUser>>(
            UserManager,
            contextAccessor,
            claimsFactory,
            null,
            null,
            null,
            null);

        Service = new NetptuneAuthService(
            Configuration,
            UserManager,
            SignInManager,
            Email,
            ContextAccessor,
            Identity,
            UnitOfWork,
            WorkspacePermissionCache,
            Cache
        );
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    // LogIn

    [Fact]
    public async Task LogIn_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        var user = AutoFixtures.AppUser;
        var request = new TokenRequest { Email = user.Email!, Password = "password" };

        UserManager.FindByEmailAsync(request.Email).Returns(user);
        SignInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(SignInResult.Success);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await Service.LogIn(request);

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
        result.Ticket!.EmailAddress.Should().Be(user.Email);
    }

    [Fact]
    public async Task LogIn_ShouldIssueJwtWithExpectedIssuerAudienceAndClaims_WhenCredentialsAreValid()
    {
        var user = AutoFixtures.AppUser;
        var request = new TokenRequest { Email = user.Email!, Password = "password" };

        UserManager.FindByEmailAsync(request.Email).Returns(user);
        SignInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(SignInResult.Success);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await Service.LogIn(request);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Ticket!.Token.Should().BeOfType<string>().Subject);

        token.Issuer.Should().Be("test-issuer");
        token.Audiences.Should().Contain("test-issuer");
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        token.ValidTo.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task LogIn_ShouldReturnFailure_WhenUserNotFound()
    {
        var request = new TokenRequest { Email = "missing@example.com", Password = "password" };

        UserManager.FindByEmailAsync(request.Email).ReturnsNull();

        var result = await Service.LogIn(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Username or password is incorrect");
    }

    [Fact]
    public async Task LogIn_ShouldReturnFailure_WhenPasswordIsNull()
    {
        var user = AutoFixtures.AppUser;
        var request = new TokenRequest { Email = user.Email!, Password = null };

        UserManager.FindByEmailAsync(request.Email).Returns(user);

        var result = await Service.LogIn(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Username or password is incorrect");
    }

    [Fact]
    public async Task LogIn_ShouldReturnFailure_WhenPasswordSignInFails()
    {
        var user = AutoFixtures.AppUser;
        var request = new TokenRequest { Email = user.Email!, Password = "wrong-password" };

        UserManager.FindByEmailAsync(request.Email).Returns(user);
        SignInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(SignInResult.Failed);

        var result = await Service.LogIn(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Username or password is incorrect");
    }

    [Fact]
    public async Task LogIn_ShouldUpdateUser_WhenLoginSucceeds()
    {
        var user = AutoFixtures.AppUser;
        var request = new TokenRequest { Email = user.Email!, Password = "password" };

        UserManager.FindByEmailAsync(request.Email).Returns(user);
        SignInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(SignInResult.Success);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        await Service.LogIn(request);

        await UserManager.Received(1).UpdateAsync(user);
    }

    // Refresh

    [Fact]
    public async Task Refresh_ShouldReturnSuccessAndRotateToken_WhenRefreshTokenIsActive()
    {
        const string rawRefreshToken = "refresh-token";
        var hashedToken = HashToken(rawRefreshToken);
        var user = AutoFixtures.AppUser;
        var existingToken = new RefreshToken
        {
            Token = hashedToken,
            UserId = user.Id,
            Created = DateTime.UtcNow.AddDays(-1),
            Expires = DateTime.UtcNow.AddDays(1),
        };

        UnitOfWork.RefreshTokens.GetByTokenAsync(hashedToken, Arg.Any<CancellationToken>()).Returns(existingToken);
        UserManager.FindByIdAsync(user.Id).Returns(user);

        var result = await Service.Refresh(new RefreshTokenRequest { RefreshToken = rawRefreshToken });

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
        result.Ticket!.UserId.Should().Be(user.Id);
        result.Ticket.RefreshToken.Should().NotBe(rawRefreshToken);

        await UnitOfWork.RefreshTokens.Received(1).RevokeAsync(hashedToken, Arg.Any<CancellationToken>());
        await UnitOfWork.RefreshTokens.Received(1).RemoveExpiredAsync(user.Id, Arg.Any<CancellationToken>());
        await UnitOfWork.RefreshTokens.Received(1).AddAsync(Arg.Is<RefreshToken>(token =>
            token.UserId == user.Id &&
            token.Token != rawRefreshToken &&
            token.Expires > DateTime.UtcNow), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_ShouldReturnFailure_WhenTokenIsMissing()
    {
        const string rawRefreshToken = "missing-token";
        var hashedToken = HashToken(rawRefreshToken);

        UnitOfWork.RefreshTokens.GetByTokenAsync(hashedToken, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await Service.Refresh(new RefreshTokenRequest { RefreshToken = rawRefreshToken });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Invalid or expired refresh token");
        await UnitOfWork.RefreshTokens.DidNotReceive().RevokeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await UnitOfWork.RefreshTokens.DidNotReceive().RemoveExpiredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_ShouldReturnFailure_WhenTokenIsExpired()
    {
        const string rawRefreshToken = "expired-token";
        var hashedToken = HashToken(rawRefreshToken);
        var existingToken = new RefreshToken
        {
            Token = hashedToken,
            UserId = "user-123",
            Created = DateTime.UtcNow.AddDays(-31),
            Expires = DateTime.UtcNow.AddSeconds(-1),
        };

        UnitOfWork.RefreshTokens.GetByTokenAsync(hashedToken, Arg.Any<CancellationToken>()).Returns(existingToken);

        var result = await Service.Refresh(new RefreshTokenRequest { RefreshToken = rawRefreshToken });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Invalid or expired refresh token");
        await UserManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
        await UnitOfWork.RefreshTokens.DidNotReceive().RevokeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_ShouldReturnFailure_WhenTokenUserNoLongerExists()
    {
        const string rawRefreshToken = "orphaned-token";
        var hashedToken = HashToken(rawRefreshToken);
        var existingToken = new RefreshToken
        {
            Token = hashedToken,
            UserId = "missing-user",
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(1),
        };

        UnitOfWork.RefreshTokens.GetByTokenAsync(hashedToken, Arg.Any<CancellationToken>()).Returns(existingToken);
        UserManager.FindByIdAsync(existingToken.UserId).ReturnsNull();

        var result = await Service.Refresh(new RefreshTokenRequest { RefreshToken = rawRefreshToken });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Invalid or expired refresh token");
        await UnitOfWork.RefreshTokens.DidNotReceive().RevokeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await UnitOfWork.RefreshTokens.DidNotReceive().RemoveExpiredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // LogInViaProvider

    [Fact]
    public async Task LogInViaProvider_ShouldReturnSuccess_WhenUserExists()
    {
        const string providerScheme = "GitHub";
        const string providerKey = "github-123";
        var user = AutoFixtures.AppUser;

        Identity.GetCurrentUserEmail().Returns(user.Email!);
        Identity.GetProviderKey().Returns(providerKey);
        UserManager.FindByLoginAsync(providerScheme, providerKey).Returns(user);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await Service.LogInViaProvider(providerScheme);

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
    }

    [Fact]
    public async Task LogInViaProvider_ShouldRegisterUser_WhenUserDoesNotExist()
    {
        const string providerScheme = "GitHub";
        const string providerKey = "github-123";
        var user = AutoFixtures.AppUser;

        Identity.GetCurrentUserEmail().Returns(user.Email!);
        Identity.GetProviderKey().Returns(providerKey);
        Identity.GetUserName().Returns($"{user.Firstname} {user.Lastname}");
        Identity.GetPictureUrl().Returns(user.PictureUrl);

        UserManager.FindByLoginAsync(providerScheme, providerKey)
            .Returns(null, user);
        UserManager.FindByEmailAsync(user.Email!).ReturnsNull();
        UserManager.FindByEmailAsync(user.Email!).Returns(null, user);

        UserManager.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        UserManager.AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<UserLoginInfo>()).Returns(IdentityResult.Success);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("confirmation-token");

        var result = await Service.LogInViaProvider(providerScheme);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LogInViaProvider_ShouldReturnLinkRequired_WhenEmailBelongsToExistingUser()
    {
        const string providerScheme = "GitHub";
        const string providerKey = "github-123";
        var user = AutoFixtures.AppUser;

        Identity.GetCurrentUserEmail().Returns(user.Email!);
        Identity.GetProviderKey().Returns(providerKey);
        Identity.GetUserName().Returns($"{user.Firstname} {user.Lastname}");
        Identity.GetPictureUrl().Returns(user.PictureUrl);

        UserManager.FindByLoginAsync(providerScheme, providerKey).ReturnsNull();
        UserManager.FindByEmailAsync(user.Email!).Returns(user);
        Cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<PendingExternalLogin>(),
                Arg.Any<DistributedCacheEntryOptions>())
            .Returns(Task.CompletedTask);

        var result = await Service.LogInViaProvider(providerScheme);

        result.IsSuccess.Should().BeFalse();
        result.IsLinkRequired.Should().BeTrue();
        result.ExternalLoginLink.Should().NotBeNull();
        result.ExternalLoginLink!.Provider.Should().Be(providerScheme);
        result.ExternalLoginLink.Email.Should().Be(user.Email);
        result.ExternalLoginLink.Token.Should().NotBeNullOrWhiteSpace();
        await Cache.Received(1).SetAsync(
            Arg.Is<string>(key => key.StartsWith("auth:external-link:", StringComparison.Ordinal)),
            Arg.Is<PendingExternalLogin>(pending =>
                pending.ExistingUserId == user.Id &&
                pending.Provider == providerScheme &&
                pending.ProviderKey == providerKey &&
                pending.Email == user.Email),
            Arg.Is<DistributedCacheEntryOptions>(options =>
                options.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(10)));
        await UserManager.DidNotReceive().AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<UserLoginInfo>());
    }

    [Fact]
    public async Task LinkProvider_ShouldReturnSuccess_WhenPendingLinkBelongsToCurrentUser()
    {
        const string providerScheme = "GitHub";
        const string providerKey = "github-123";
        const string token = "pending-link-token";
        var user = AutoFixtures.AppUser;
        var pending = new PendingExternalLogin
        {
            ExistingUserId = user.Id,
            Provider = providerScheme,
            ProviderKey = providerKey,
            Email = user.Email!,
            Created = DateTime.UtcNow,
        };

        Identity.GetCurrentUserId().Returns(user.Id);
        Cache.GetValueAsync<PendingExternalLogin>(GetExternalLinkCacheKey(token)).Returns(pending);
        UserManager.FindByIdAsync(user.Id).Returns(user);
        UserManager.AddLoginAsync(user, Arg.Any<UserLoginInfo>()).Returns(IdentityResult.Success);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await Service.LinkProvider(new LinkProviderRequest { Token = token });

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
        result.Ticket!.UserId.Should().Be(user.Id);
        await UserManager.Received(1).AddLoginAsync(user, Arg.Is<UserLoginInfo>(login =>
            login.LoginProvider == providerScheme &&
            login.ProviderKey == providerKey &&
            login.ProviderDisplayName == providerScheme));
        await Cache.Received(1).RemoveAsync(GetExternalLinkCacheKey(token));
    }

    [Fact]
    public async Task LinkProvider_ShouldReturnFailure_WhenPendingLinkIsMissing()
    {
        const string token = "missing-link-token";

        Cache.GetValueAsync<PendingExternalLogin>(GetExternalLinkCacheKey(token)).ReturnsNull();

        var result = await Service.LinkProvider(new LinkProviderRequest { Token = token });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("External login link is invalid or expired.");
        await UserManager.DidNotReceive().AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<UserLoginInfo>());
    }

    [Fact]
    public async Task LinkProvider_ShouldReturnFailure_WhenCurrentUserDoesNotMatchPendingLink()
    {
        const string token = "wrong-user-link-token";
        var pending = new PendingExternalLogin
        {
            ExistingUserId = "expected-user",
            Provider = "GitHub",
            ProviderKey = "github-123",
            Email = "user@example.com",
            Created = DateTime.UtcNow,
        };

        Identity.GetCurrentUserId().Returns("different-user");
        Cache.GetValueAsync<PendingExternalLogin>(GetExternalLinkCacheKey(token)).Returns(pending);

        var result = await Service.LinkProvider(new LinkProviderRequest { Token = token });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("External login link does not belong to the signed-in account.");
        await UserManager.DidNotReceive().AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<UserLoginInfo>());
    }

    [Fact]
    public async Task LinkProvider_ShouldReturnFailure_WhenExistingEmailCannotBeLinked()
    {
        const string token = "failed-link-token";
        var user = AutoFixtures.AppUser;
        var pending = new PendingExternalLogin
        {
            ExistingUserId = user.Id,
            Provider = "GitHub",
            ProviderKey = "github-123",
            Email = user.Email!,
            Created = DateTime.UtcNow,
        };

        Identity.GetCurrentUserId().Returns(user.Id);
        Cache.GetValueAsync<PendingExternalLogin>(GetExternalLinkCacheKey(token)).Returns(pending);
        UserManager.FindByIdAsync(user.Id).Returns(user);
        UserManager.AddLoginAsync(user, Arg.Any<UserLoginInfo>())
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateLogin",
                Description = "Login already exists",
            }));

        var result = await Service.LinkProvider(new LinkProviderRequest { Token = token });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Login already exists");
    }

    private static string GetExternalLinkCacheKey(string token)
    {
        return $"auth:external-link:{HashToken(token)}";
    }

    // Register

    [Fact]
    public async Task Register_ShouldReturnSuccess_WhenRequestIsValid()
    {
        var user = AutoFixtures.AppUser;
        var request = new RegisterRequest
        {
            Email = user.Email!,
            Password = "password123",
            Firstname = user.Firstname,
            Lastname = user.Lastname,
        };

        UserManager.FindByEmailAsync(request.Email).Returns(null, user);
        UserManager.CreateAsync(Arg.Any<AppUser>(), request.Password).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("email-token");
        SignInManager.SignInAsync(Arg.Any<AppUser>(), false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_ShouldReturnFailure_WhenPasswordIsNullAndNoOAuthProvider()
    {
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = null,
            Firstname = "John",
            Lastname = "Doe",
        };

        UserManager.FindByEmailAsync(request.Email).ReturnsNull();

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Invalid request.");
    }

    [Fact]
    public async Task Register_ShouldReturnFailure_WhenUserAlreadyExists()
    {
        var existingUser = AutoFixtures.AppUser;
        var request = new RegisterRequest
        {
            Email = existingUser.Email!,
            Password = "password123",
            Firstname = "John",
            Lastname = "Doe",
        };

        UserManager.FindByEmailAsync(request.Email).Returns(existingUser);

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("User with email already exists");
    }

    [Fact]
    public async Task Register_ShouldReturnFailure_WhenInviteCodeIsInvalidOrExpired()
    {
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "password123",
            Firstname = "John",
            Lastname = "Doe",
            InviteCode = "invalid-code",
        };

        UnitOfWork.WorkspaceInvites.GetByCode("invalid-code", Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Invite code is invalid/expired.");
    }

    [Fact]
    public async Task Register_ShouldCreateUserWithoutPassword_WhenOAuthProviderIsSet()
    {
        const string providerScheme = "GitHub";
        const string providerKey = "github-123";
        var user = AutoFixtures.AppUser;
        var request = new RegisterRequest
        {
            Email = user.Email!,
            Password = null,
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            OAuthProvider = providerScheme,
            OAuthProviderKey = providerKey,
        };

        UserManager.FindByEmailAsync(request.Email).Returns(null, user);
        UserManager.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        UserManager.AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<UserLoginInfo>()).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("email-token");
        SignInManager.SignInAsync(Arg.Any<AppUser>(), false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeTrue();
        await UserManager.Received(1).CreateAsync(Arg.Any<AppUser>());
        await UserManager.DidNotReceive().CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>());
        await UserManager.Received(1).AddLoginAsync(Arg.Any<AppUser>(), Arg.Is<UserLoginInfo>(l =>
            l.LoginProvider == providerScheme && l.ProviderKey == providerKey));
    }

    [Fact]
    public async Task Register_ShouldAddToWorkspace_WhenInviteCodeIsValid()
    {
        var user = AutoFixtures.AppUser;
        var invite = new RelationshipInvite { Email = user.Email!, WorkspaceId = 42, Code = "valid-code" };
        var request = new RegisterRequest
        {
            Email = user.Email!,
            Password = "password123",
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            InviteCode = "valid-code",
        };

        UnitOfWork.WorkspaceInvites.GetByCode("valid-code", Arg.Any<CancellationToken>()).Returns(invite);
        UserManager.FindByEmailAsync(request.Email).Returns(null, user);
        UserManager.CreateAsync(Arg.Any<AppUser>(), request.Password).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("email-token");
        SignInManager.SignInAsync(Arg.Any<AppUser>(), false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        UnitOfWork.WorkspaceUsers.AddAsync(Arg.Any<WorkspaceAppUser>(), Arg.Any<CancellationToken>()).Returns(new WorkspaceAppUser());
        UnitOfWork.CompleteAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.WorkspaceUsers.Received(1).AddAsync(Arg.Is<WorkspaceAppUser>(w => w.WorkspaceId == 42), Arg.Any<CancellationToken>());
        await UnitOfWork.Received().CompleteAsync(Arg.Any<CancellationToken>());
        await UnitOfWork.WorkspaceInvites.Received(1).Accept("valid-code", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_ShouldSendWelcomeEmail_WhenRegistrationSucceeds()
    {
        var user = AutoFixtures.AppUser;
        var request = new RegisterRequest
        {
            Email = user.Email!,
            Password = "password123",
            Firstname = user.Firstname,
            Lastname = user.Lastname,
        };

        UserManager.FindByEmailAsync(request.Email).Returns(null, user);
        UserManager.CreateAsync(Arg.Any<AppUser>(), request.Password).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("email-token");
        SignInManager.SignInAsync(Arg.Any<AppUser>(), false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);

        await Service.Register(request);

        await Email.Received(1).Send(Arg.Any<SendEmailModel>());
    }

    // ConfirmEmail

    [Fact]
    public async Task ConfirmEmail_ShouldReturnSuccess_WhenUserAndCodeAreValid()
    {
        var user = AutoFixtures.AppUser;

        UserManager.FindByIdAsync(user.Id).Returns(user);
        UserManager.ConfirmEmailAsync(user, "valid-code").Returns(IdentityResult.Success);

        var result = await Service.ConfirmEmail(user.Id, "valid-code");

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmEmail_ShouldReturnFailure_WhenUserNotFound()
    {
        UserManager.FindByIdAsync("missing-id").ReturnsNull();

        var result = await Service.ConfirmEmail("missing-id", "code");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmail_ShouldReturnFailure_WhenCodeIsInvalid()
    {
        var user = AutoFixtures.AppUser;

        UserManager.FindByIdAsync(user.Id).Returns(user);
        UserManager.ConfirmEmailAsync(user, "bad-code")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var result = await Service.ConfirmEmail(user.Id, "bad-code");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmail_WithAppUser_ShouldReturnSuccess_WhenCodeIsValid()
    {
        var user = AutoFixtures.AppUser;

        UserManager.ConfirmEmailAsync(user, "valid-code").Returns(IdentityResult.Success);

        var result = await Service.ConfirmEmail(user, "valid-code");

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
    }

    // RequestPasswordReset

    [Fact]
    public async Task RequestPasswordReset_ShouldReturnSuccess_WhenUserExists()
    {
        var user = AutoFixtures.AppUser;
        var request = new RequestPasswordResetRequest { Email = user.Email! };

        UserManager.FindByEmailAsync(request.Email).Returns(user);
        UserManager.GeneratePasswordResetTokenAsync(user).Returns("reset-token");

        var result = await Service.RequestPasswordReset(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldSendEmail_WhenUserExists()
    {
        var user = AutoFixtures.AppUser;
        var request = new RequestPasswordResetRequest { Email = user.Email! };

        UserManager.FindByEmailAsync(request.Email).Returns(user);
        UserManager.GeneratePasswordResetTokenAsync(user).Returns("reset-token");

        await Service.RequestPasswordReset(request);

        await Email.Received(1).Send(Arg.Any<SendEmailModel>());
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldReturnFailure_WhenUserNotFound()
    {
        var request = new RequestPasswordResetRequest { Email = "missing@example.com" };

        UserManager.FindByEmailAsync(request.Email).ReturnsNull();

        var result = await Service.RequestPasswordReset(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldNotSendEmail_WhenUserNotFound()
    {
        var request = new RequestPasswordResetRequest { Email = "missing@example.com" };

        UserManager.FindByEmailAsync(request.Email).ReturnsNull();

        await Service.RequestPasswordReset(request);

        await Email.DidNotReceive().Send(Arg.Any<SendEmailModel>());
    }

    // ResetPassword

    [Fact]
    public async Task ResetPassword_ShouldReturnSuccess_WhenRequestIsValid()
    {
        var user = AutoFixtures.AppUser;
        var request = new ResetPasswordRequest { UserId = user.Id, Code = "valid-code", Password = "newpassword" };

        UserManager.FindByIdAsync(user.Id).Returns(user);
        UserManager.ResetPasswordAsync(user, request.Code, request.Password).Returns(IdentityResult.Success);
        SignInManager.SignInAsync(user, false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await Service.ResetPassword(request);

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnFailure_WhenUserNotFound()
    {
        var request = new ResetPasswordRequest { UserId = "missing-id", Code = "code", Password = "newpassword" };

        UserManager.FindByIdAsync("missing-id").ReturnsNull();

        var result = await Service.ResetPassword(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Password Reset Failed, userId or code was invalid");
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnFailure_WhenCodeIsInvalid()
    {
        var user = AutoFixtures.AppUser;
        var request = new ResetPasswordRequest { UserId = user.Id, Code = "bad-code", Password = "newpassword" };

        UserManager.FindByIdAsync(user.Id).Returns(user);
        UserManager.ResetPasswordAsync(user, request.Code, request.Password)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var result = await Service.ResetPassword(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Password Reset Failed, userId or code was invalid");
    }

    // ChangePassword

    [Fact]
    public async Task ChangePassword_ShouldReturnSuccess_WhenRequestIsValid()
    {
        var user = AutoFixtures.AppUser;
        var request = new ChangePasswordRequest
        {
            UserId = user.Id,
            CurrentPassword = "currentpassword",
            NewPassword = "newpassword",
        };

        UserManager.FindByIdAsync(user.Id).Returns(user);
        UserManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword).Returns(IdentityResult.Success);
        SignInManager.SignInAsync(user, false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await Service.ChangePassword(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnFailure_WhenUserNotFound()
    {
        var request = new ChangePasswordRequest
        {
            UserId = "missing-id",
            CurrentPassword = "currentpassword",
            NewPassword = "newpassword",
        };

        UserManager.FindByIdAsync("missing-id").ReturnsNull();

        var result = await Service.ChangePassword(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnFailure_WhenCurrentPasswordIsWrong()
    {
        var user = AutoFixtures.AppUser;
        var request = new ChangePasswordRequest
        {
            UserId = user.Id,
            CurrentPassword = "wrong-password",
            NewPassword = "newpassword",
        };

        UserManager.FindByIdAsync(user.Id).Returns(user);
        UserManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

        var result = await Service.ChangePassword(request);

        result.IsSuccess.Should().BeFalse();
    }

    // CurrentUser

    [Fact]
    public async Task CurrentUser_ShouldReturnNull_WhenPrincipalIsNull()
    {
        ContextAccessor.HttpContext.ReturnsNull();

        var result = await Service.CurrentUser();

        result.Should().BeNull();
    }

    [Fact]
    public async Task CurrentUser_ShouldReturnCorrectly_WhenUserIsAuthenticated()
    {
        var user = AutoFixtures.AppUser;
        var httpContext = Substitute.For<HttpContext>();
        var principal = new ClaimsPrincipal();
        const string workspaceKey = "workspace-key";

        httpContext.User.Returns(principal);
        ContextAccessor.HttpContext.Returns(httpContext);
        UserManager.GetUserAsync(principal).Returns(user);
        Identity.TryGetWorkspaceKey().Returns(workspaceKey);
        WorkspacePermissionCache.GetUserPermissions(user.Id, workspaceKey).Returns(new UserPermissions
        {
           Permissions = [],
           Role = WorkspaceRole.Owner,
           UserId = user.Id,
           WorkspaceKey = workspaceKey,
        });

        var result = await Service.CurrentUser();

        result.Should().NotBeNull();
        result.EmailAddress.Should().Be(user.Email);
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CurrentUser_ShouldReturnNull_WhenUserManagerReturnsNull()
    {
        var httpContext = Substitute.For<HttpContext>();
        var principal = new ClaimsPrincipal();

        httpContext.User.Returns(principal);
        ContextAccessor.HttpContext.Returns(httpContext);
        UserManager.GetUserAsync(principal).ReturnsNull();

        var result = await Service.CurrentUser();

        result.Should().BeNull();
    }

    [Fact]
    public async Task CurrentUser_ShouldReturnNull_WhenWorkspacePermissionsAreMissing()
    {
        var user = AutoFixtures.AppUser;
        var httpContext = Substitute.For<HttpContext>();
        var principal = new ClaimsPrincipal();
        const string workspaceKey = "workspace-key";

        httpContext.User.Returns(principal);
        ContextAccessor.HttpContext.Returns(httpContext);
        UserManager.GetUserAsync(principal).Returns(user);
        Identity.TryGetWorkspaceKey().Returns(workspaceKey);
        WorkspacePermissionCache.GetUserPermissions(user.Id, workspaceKey).ReturnsNull();

        var result = await Service.CurrentUser();

        result.Should().BeNull();
    }

    // ValidateInviteCode

    [Fact]
    public async Task ValidateInviteCode_ShouldReturnInvite_WhenCodeIsValid()
    {
        var invite = new RelationshipInvite { Email = "user@example.com", WorkspaceId = 1, Code = "valid-code" };

        UnitOfWork.WorkspaceInvites.GetByCode("valid-code", Arg.Any<CancellationToken>()).Returns(invite);

        var result = await Service.ValidateInviteCode("valid-code");

        result.Should().NotBeNull();
        result.Email.Should().Be(invite.Email);
        result.WorkspaceId.Should().Be(invite.WorkspaceId);
    }

    [Fact]
    public async Task ValidateInviteCode_ShouldReturnNull_WhenCodeIsExpiredOrInvalid()
    {
        UnitOfWork.WorkspaceInvites.GetByCode("expired-code", Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await Service.ValidateInviteCode("expired-code");

        result.Should().BeNull();
    }

    // GetLoginProviders

    [Fact]
    public async Task GetLoginProviders_ShouldReturnProviderNames_WhenUserHasLinkedProviders()
    {
        var user = AutoFixtures.AppUser;
        var logins = new List<UserLoginInfo>
        {
            new("GitHub", "github-key-123", "GitHub"),
            new("Google", "google-key-456", "Google"),
        };

        Identity.GetCurrentUser().Returns(user);
        UserManager.GetLoginsAsync(user).Returns(logins);

        var result = await Service.GetLoginProviders();

        result.Should().BeEquivalentTo(["GitHub", "Google"]);
    }

    [Fact]
    public async Task GetLoginProviders_ShouldReturnEmptyList_WhenUserHasNoLinkedProviders()
    {
        var user = AutoFixtures.AppUser;

        Identity.GetCurrentUser().Returns(user);
        UserManager.GetLoginsAsync(user).Returns([]);

        var result = await Service.GetLoginProviders();

        result.Should().BeEmpty();
    }
}
