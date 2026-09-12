using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Netptune.App.Utility;
using Netptune.Core.Authentication.Models;

using Xunit;

namespace Netptune.UnitTests.Netptune.App.Utility;

public class CookieHelperTests
{
    [Fact]
    public void SetAuthCookies_UsesOAuthCompatibleSameSitePolicy()
    {
        var context = new DefaultHttpContext();
        var ticket = new AuthenticationTicket
        {
            UserId = "user-id",
            EmailAddress = "user@example.com",
            DisplayName = "User",
            Token = "access-token",
            RefreshToken = "refresh-token",
            Issued = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(15),
        };

        CookieHelper.SetAuthCookies(context, ticket);

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();

        setCookieHeaders.Should().HaveCount(2);
        setCookieHeaders.Should().OnlyContain(cookie =>
            cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("secure", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SetAuthCookies_IssuesSessionCookies_WhenTheUserDidNotAskToStaySignedIn()
    {
        var context = new DefaultHttpContext();

        CookieHelper.SetAuthCookies(context, NewTicket(), keepSignedIn: false);

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();

        setCookieHeaders.Should().NotContain(cookie => cookie.Contains("expires", StringComparison.OrdinalIgnoreCase));
        setCookieHeaders.Should().ContainSingle(cookie => cookie.StartsWith("auth_persist=0", StringComparison.Ordinal));
    }

    [Fact]
    public void SetAuthCookies_KeepsTheRefreshCookieForThirtyDays_WhenTheUserAskedToStaySignedIn()
    {
        var context = new DefaultHttpContext();

        CookieHelper.SetAuthCookies(context, NewTicket());

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        var refreshCookie = setCookieHeaders.Single(cookie => cookie!.StartsWith("refresh_token=", StringComparison.Ordinal));

        refreshCookie.Should().Contain("expires=");
        setCookieHeaders.Should().NotContain(cookie => cookie.StartsWith("auth_persist=", StringComparison.Ordinal));
    }

    [Fact]
    public void SetAuthCookies_ClearsTheMarker_WhenAStaySignedInLoginFollowsASessionOnlyOne()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie = "auth_persist=0";

        CookieHelper.SetAuthCookies(context, NewTicket());

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();

        setCookieHeaders.Should().ContainSingle(cookie => cookie.StartsWith("auth_persist=;", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("auth_persist=0", false)]
    public void ShouldKeepSignedIn_ReadsTheMarkerCookie(string cookieHeader, bool expected)
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Cookie = cookieHeader;

        CookieHelper.ShouldKeepSignedIn(context.Request).Should().Be(expected);
    }

    private static AuthenticationTicket NewTicket()
    {
        return new AuthenticationTicket
        {
            UserId = "user-id",
            EmailAddress = "user@example.com",
            DisplayName = "User",
            Token = "access-token",
            RefreshToken = "refresh-token",
            Issued = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(15),
        };
    }
}
