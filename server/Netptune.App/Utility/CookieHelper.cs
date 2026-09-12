using Netptune.Core.Authentication.Models;

namespace Netptune.App.Utility;

public static class CookieHelper
{
    private const string RefreshPath = "/api/auth/refresh";
    private const string PersistenceCookie = "auth_persist";
    private const string SessionOnly = "0";

    public static void SetAuthCookies(HttpContext context, AuthenticationTicket ticket, bool keepSignedIn = true)
    {
        var response = context.Response;

        response.Cookies.Append("access_token", ticket.Token.ToString()!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api",
            Expires = keepSignedIn ? ticket.Expires : null,
        });

        response.Cookies.Append("refresh_token", ticket.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = RefreshPath,
            Expires = keepSignedIn ? DateTimeOffset.UtcNow.AddDays(30) : null,
        });

        if (!keepSignedIn)
        {
            response.Cookies.Append(PersistenceCookie, SessionOnly, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = RefreshPath,
            });

            return;
        }

        var carriesMarker = context.Request.Cookies.ContainsKey(PersistenceCookie);

        if (carriesMarker)
        {
            response.Cookies.Delete(PersistenceCookie, new CookieOptions { Path = RefreshPath });
        }
    }

    public static bool ShouldKeepSignedIn(HttpRequest request)
    {
        return request.Cookies[PersistenceCookie] != SessionOnly;
    }

    public static void ClearAuthCookies(HttpResponse response)
    {
        response.Cookies.Delete("access_token", new CookieOptions { Path = "/api" });
        response.Cookies.Delete("refresh_token", new CookieOptions { Path = RefreshPath });
        response.Cookies.Delete(PersistenceCookie, new CookieOptions { Path = RefreshPath });
    }
}
