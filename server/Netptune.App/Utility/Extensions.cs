using System.Net;

namespace Netptune.App.Utility;

public static class Extensions
{
    public static string? GetRemoteIpAddress(this HttpContext context)
    {
        return context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
               ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
               ?? context.Connection.RemoteIpAddress?.ToString();
    }
}
