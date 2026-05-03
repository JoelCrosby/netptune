using Netptune.Core.Authentication.Models;

namespace Netptune.App.Utility;

public static class CookieHelper
{
    public static void SetAuthCookies(HttpResponse response, AuthenticationTicket ticket)
    {
        response.Cookies.Append("access_token", ticket.Token.ToString()!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api",
            Expires = ticket.Expires,
        });

        response.Cookies.Append("refresh_token", ticket.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth/refresh",
            Expires = DateTimeOffset.UtcNow.AddDays(30),
        });
    }

    public static void ClearAuthCookies(HttpResponse response)
    {
        response.Cookies.Delete("access_token", new CookieOptions { Path = "/api" });
        response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth/refresh" });
    }
}
