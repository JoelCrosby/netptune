using System.Security.Claims;
using System.Threading.RateLimiting;

using Netptune.Core.Authorization;

namespace Netptune.PublicApi.Configuration;

public static class PublicApiRateLimiter
{
    public const string PolicyName = "public-api";

    public static IServiceCollection AddPublicApiRateLimiter(this IServiceCollection services)
    {
        services.AddSingleton<PreAuthenticationRateLimiter>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PolicyName, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetPartitionKey(context),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10,
                    }));
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context)
    {
        var credentialId = context.User.FindFirstValue(NetptuneClaims.CredentialId);

        if (!string.IsNullOrEmpty(credentialId))
        {
            return $"credential:{credentialId}";
        }

        var connectingIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        var forwardedFor =  context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var forwardedKey = connectingIp ?? forwardedFor;

        return $"ip:{forwardedKey ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
