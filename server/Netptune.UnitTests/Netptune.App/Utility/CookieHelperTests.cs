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

        CookieHelper.SetAuthCookies(context.Response, ticket);

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();

        setCookieHeaders.Should().HaveCount(2);
        setCookieHeaders.Should().OnlyContain(cookie =>
            cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("secure", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }
}
