using System.Security.Authentication;
using System.Security.Claims;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using Netptune.Core.Cache;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class IdentityServiceTests
{
    private readonly IdentityService Service;

    private readonly IUserCache UserCache = Substitute.For<IUserCache>();
    private readonly IWorkspaceCache WorkspaceCache = Substitute.For<IWorkspaceCache>();
    private readonly IHttpContextAccessor HttpContextAccessor = Substitute.For<IHttpContextAccessor>();

    public IdentityServiceTests()
    {
        Service = new(UserCache, WorkspaceCache, HttpContextAccessor);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnCorrectly_WhenClaimAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(new [] { new Claim(ClaimTypes.NameIdentifier, "userId") }),
        }));

        var result = await Service.GetCurrentUser();

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(user);
    }

    [Fact]
    public void GetCurrentUser_ShouldReturnThrow_WhenClaimNotAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(Array.Empty<Claim>()),
        }));

       var act = () => Service.GetCurrentUser().ThrowsAsync<AuthenticationException>();

       act.Should().Throw<AuthenticationException>();
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnCorrectly_WhenClaimAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(new [] { new Claim(ClaimTypes.NameIdentifier, "userId") }),
        }));

        var result = Service.GetCurrentUserId();

        result.Should().Be("userId");
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnThrow_WhenClaimNotAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(Array.Empty<Claim>()),
        }));

        var act = () => Service.GetCurrentUserId().Throws<AuthenticationException>();

        act.Should().Throw<AuthenticationException>();
    }

    [Fact]
    public void GetCurrentUserEmail_ShouldReturnCorrectly_WhenClaimAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(new [] { new Claim(ClaimTypes.Email, "user@email.com") }),
        }));

        var result = Service.GetCurrentUserEmail();

        result.Should().Be("user@email.com");
    }

    [Fact]
    public void GetCurrentUserEmail_ShouldReturnThrow_WhenClaimNotAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(Array.Empty<Claim>()),
        }));

        var act = () => Service.GetCurrentUserEmail().Throws<AuthenticationException>();

        act.Should().Throw<AuthenticationException>();
    }

    [Fact]
    public void GetUserName_ShouldReturnCorrectly_WhenClaimAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(new [] { new Claim(ClaimTypes.Name, "username") }),
        }));

        var result = Service.GetUserName();

        result.Should().Be("username");
    }

    [Fact]
    public void GetUserName_ShouldReturnGitHubValue_WhenGitHubClaimAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(new [] { new Claim(ClaimTypes.Name, "username") }),
            new ClaimsIdentity(new [] { new Claim("urn:github:name", "github-username") }),
        }));

        var result = Service.GetUserName();

        result.Should().Be("github-username");
    }

    [Fact]
    public void GetUserName_ShouldReturnThrow_WhenClaimNotAvailable()
    {
        var user = AutoFixtures.AppUser;

        UserCache.Get(Arg.Any<string>()).Returns(user);

        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(Array.Empty<Claim>()),
        }));

        var act = () => Service.GetUserName().Throws<AuthenticationException>();

        act.Should().Throw<AuthenticationException>();
    }

    [Fact]
    public void GetPictureUrl_ShouldReturnCorrectly_WhenClaimAvailable()
    {
        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(new [] { new Claim("Provider-Picture-Url", "picture") }),
        }));

        var result = Service.GetPictureUrl();

        result.Should().Be("picture");
    }

    [Fact]
    public void GetPictureUrl_ShouldReturnThrow_WhenClaimNotAvailable()
    {
        HttpContextAccessor.HttpContext?.User.Returns(new ClaimsPrincipal(new []
        {
            new ClaimsIdentity(Array.Empty<Claim>()),
        }));

        var act = () => Service.GetPictureUrl().Throws<AuthenticationException>();

        act.Should().Throw<AuthenticationException>();
    }

    [Fact]
    public void GetWorkspaceKey_ShouldReturnCorrectly_WhenClaimAvailable()
    {
        HttpContextAccessor.HttpContext?.Request.Headers
            .TryGetValue("workspace", out Arg.Any<StringValues>())
            .Returns(x =>
            {
                x[1] = new StringValues("workspace-value");
                return true;
            });

        var result = Service.GetWorkspaceKey();

        result.Should().Be("workspace-value");
    }

    [Fact]
    public void GetWorkspaceKey_ShouldReturnThrow_WhenHeaderNotAvailable()
    {
        HttpContextAccessor.HttpContext?.Request.Headers
            .TryGetValue("workspace", out Arg.Any<StringValues>())
            .Returns(x =>
            {
                x[1] = default;
                return false;
            });

        var act = () => Service.GetWorkspaceKey();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void GetWorkspaceKey_ShouldReturnThrow_WhenContextIsNull()
    {
        HttpContextAccessor.HttpContext.ReturnsNull();

        var act = () => Service.GetWorkspaceKey();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GetWorkspaceId_ShouldReturnCorrectly_WhenClaimAvailable()
    {
        HttpContextAccessor.HttpContext?.Request.Headers
            .TryGetValue("workspace", out Arg.Any<StringValues>())
            .Returns(x =>
            {
                x[1] = new StringValues("workspace-value");
                return true;
            });

        WorkspaceCache.GetIdBySlug("workspace-value").Returns(10);

        var result = await Service.GetWorkspaceId();

        result.Should().Be(10);
    }

    [Fact]
    public async Task GetWorkspaceId_ShouldThrow_WhenIdNotFound()
    {
        HttpContextAccessor.HttpContext?.Request.Headers
            .TryGetValue("workspace", out Arg.Any<StringValues>())
            .Returns(x =>
            {
                x[1] = new StringValues("workspace-value");
                return true;
            });

        WorkspaceCache.GetIdBySlug("workspace-value").ReturnsNull();

        var act = () => Service.GetWorkspaceId();

        await act.Should().ThrowAsync<Exception>();
    }
}
