using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Relationships;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Authentication;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class AuthenticationServiceTests
{
    private readonly NetptuneAuthService Service;

    private readonly UserManager<AppUser> UserManager;
    private readonly SignInManager<AppUser> SignInManager;
    private readonly IEmailService Email = Substitute.For<IEmailService>();
    private readonly IHttpContextAccessor ContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly IInviteCache InviteCache = Substitute.For<IInviteCache>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IWorkspaceService WorkspaceService = Substitute.For<IWorkspaceService>();
    private readonly IConfiguration Configuration = Substitute.For<IConfiguration>();

    public AuthenticationServiceTests()
    {
        Environment.SetEnvironmentVariable(
            "NETPTUNE_SIGNING_KEY",
             "test-signing-key-that-is-long-enough-for-hmac-sha256");

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
            InviteCache,
            Identity,
            UnitOfWork,
            WorkspaceService
        );
    }

    // LogIn

    [Fact]
    public async Task LogIn_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        var user = AutoFixtures.AppUser;
        var request = new TokenRequest { Email = user.Email!, Password = "password" };

        UserManager.FindByEmailAsync(request.Email).Returns(user);
        SignInManager.CheckPasswordSignInAsync(user, request.Password, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await Service.LogIn(request);

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
        result.Ticket!.EmailAddress.Should().Be(user.Email);
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
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);

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
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        await Service.LogIn(request);

        await UserManager.Received(1).UpdateAsync(user);
    }

    // LogInViaProvider

    [Fact]
    public async Task LogInViaProvider_ShouldReturnSuccess_WhenUserExists()
    {
        var user = AutoFixtures.AppUser;

        Identity.GetCurrentUserEmail().Returns(user.Email!);
        Identity.GetUserName().Returns($"{user.Firstname} {user.Lastname}");
        UserManager.FindByEmailAsync(user.Email!).Returns(user);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var result = await Service.LogInViaProvider();

        result.IsSuccess.Should().BeTrue();
        result.Ticket.Should().NotBeNull();
    }

    [Fact]
    public async Task LogInViaProvider_ShouldRegisterUser_WhenUserDoesNotExist()
    {
        var user = AutoFixtures.AppUser;

        Identity.GetCurrentUserEmail().Returns(user.Email!);
        Identity.GetUserName().Returns($"{user.Firstname} {user.Lastname}");
        Identity.GetPictureUrl().Returns(user.PictureUrl);

        UserManager.FindByEmailAsync(user.Email!)
            .Returns(null, user);

        UserManager.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        UserManager.UpdateAsync(user).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("confirmation-token");

        var result = await Service.LogInViaProvider();

        result.IsSuccess.Should().BeTrue();
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
            AuthenticationProvider = AuthenticationProvider.Netptune,
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
    public async Task Register_ShouldReturnFailure_WhenPasswordIsNullAndNotGitHub()
    {
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = null,
            Firstname = "John",
            Lastname = "Doe",
            AuthenticationProvider = AuthenticationProvider.Netptune,
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
            AuthenticationProvider = AuthenticationProvider.Netptune,
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
            AuthenticationProvider = AuthenticationProvider.Netptune,
        };

        InviteCache.Get("invalid-code").ReturnsNull();

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Invite code is invalid/expired.");
    }

    [Fact]
    public async Task Register_ShouldCreateUserWithoutPassword_WhenProviderIsGitHub()
    {
        var user = AutoFixtures.AppUser;
        var request = new RegisterRequest
        {
            Email = user.Email!,
            Password = null,
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            AuthenticationProvider = AuthenticationProvider.GitHub,
        };

        UserManager.FindByEmailAsync(request.Email).Returns(null, user);
        UserManager.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("email-token");
        SignInManager.SignInAsync(Arg.Any<AppUser>(), false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeTrue();
        await UserManager.Received(1).CreateAsync(Arg.Any<AppUser>());
        await UserManager.DidNotReceive().CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Register_ShouldAddToWorkspace_WhenInviteCodeIsValid()
    {
        var user = AutoFixtures.AppUser;
        var invite = new WorkspaceInvite { Email = user.Email!, WorkspaceId = 42 };
        var request = new RegisterRequest
        {
            Email = user.Email!,
            Password = "password123",
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            InviteCode = "valid-code",
            AuthenticationProvider = AuthenticationProvider.Netptune,
        };

        InviteCache.Get("valid-code").Returns(invite);
        UserManager.FindByEmailAsync(request.Email).Returns(null, user);
        UserManager.CreateAsync(Arg.Any<AppUser>(), request.Password).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("email-token");
        SignInManager.SignInAsync(Arg.Any<AppUser>(), false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        UnitOfWork.WorkspaceUsers.AddAsync(Arg.Any<WorkspaceAppUser>()).Returns(new WorkspaceAppUser());
        UnitOfWork.CompleteAsync().Returns(1);

        var result = await Service.Register(request);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.WorkspaceUsers.Received(1).AddAsync(Arg.Is<WorkspaceAppUser>(w => w.WorkspaceId == 42));
        await UnitOfWork.Received().CompleteAsync();
        InviteCache.Received(1).Remove("valid-code");
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
            AuthenticationProvider = AuthenticationProvider.Netptune,
        };

        UserManager.FindByEmailAsync(request.Email).Returns(null, user);
        UserManager.CreateAsync(Arg.Any<AppUser>(), request.Password).Returns(IdentityResult.Success);
        UserManager.GenerateEmailConfirmationTokenAsync(Arg.Any<AppUser>()).Returns("email-token");
        SignInManager.SignInAsync(Arg.Any<AppUser>(), false).Returns(Task.CompletedTask);
        UserManager.UpdateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);

        await Service.Register(request);

        await Email.Received(1).Send(Arg.Any<Core.Models.Messaging.SendEmailModel>());
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

        await Email.Received(1).Send(Arg.Any<Core.Models.Messaging.SendEmailModel>());
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

        await Email.DidNotReceive().Send(Arg.Any<Core.Models.Messaging.SendEmailModel>());
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
        var principal = new System.Security.Claims.ClaimsPrincipal();

        httpContext.User.Returns(principal);
        ContextAccessor.HttpContext.Returns(httpContext);
        UserManager.GetUserAsync(principal).Returns(user);

        var result = await Service.CurrentUser();

        result.Should().NotBeNull();
        result!.EmailAddress.Should().Be(user.Email);
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CurrentUser_ShouldReturnNull_WhenUserManagerReturnsNull()
    {
        var httpContext = Substitute.For<HttpContext>();
        var principal = new System.Security.Claims.ClaimsPrincipal();

        httpContext.User.Returns(principal);
        ContextAccessor.HttpContext.Returns(httpContext);
        UserManager.GetUserAsync(principal).ReturnsNull();

        var result = await Service.CurrentUser();

        result.Should().BeNull();
    }

    // ValidateInviteCode

    [Fact]
    public async Task ValidateInviteCode_ShouldReturnInvite_WhenCodeIsValid()
    {
        var invite = new WorkspaceInvite { Email = "user@example.com", WorkspaceId = 1 };

        InviteCache.Get("valid-code").Returns(invite);

        var result = await Service.ValidateInviteCode("valid-code");

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(invite);
    }

    [Fact]
    public async Task ValidateInviteCode_ShouldReturnNull_WhenCodeIsExpiredOrInvalid()
    {
        InviteCache.Get("expired-code").ReturnsNull();

        var result = await Service.ValidateInviteCode("expired-code");

        result.Should().BeNull();
    }
}
