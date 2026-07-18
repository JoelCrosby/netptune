using System.Security.Claims;

namespace Netptune.App.Utility;

public static class Extensions
{
    public static string? GetRemoteIpAddress(this HttpContext context)
    {
        return context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
               ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
               ?? context.Connection.RemoteIpAddress?.ToString();
    }

    public static string GetRateLimitPartitionKey(this HttpContext context)
    {
        var credentialId = context.User.FindFirstValue(Netptune.Core.Authorization.NetptuneClaims.CredentialId);

        if (!string.IsNullOrEmpty(credentialId))
        {
            return $"credential:{credentialId}";
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return $"ip:{context.GetRemoteIpAddress() ?? "unknown"}";
        }

        var workspace = context.Request.Headers["workspace"].FirstOrDefault();

        return string.IsNullOrEmpty(workspace)
            ? $"user:{userId}"
            : $"ws:{workspace}|user:{userId}";
    }
}
