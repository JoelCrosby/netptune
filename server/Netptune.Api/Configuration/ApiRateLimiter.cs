using System.Security.Claims;
using System.Threading.RateLimiting;

using Netptune.Core.Authorization;
using Netptune.ServiceDefaults.Networking;

namespace Netptune.Api.Configuration;

public static class ApiRateLimiter
{
    public const string PolicyName = "api";

    public static IServiceCollection AddApiRateLimiter(this IServiceCollection services)
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

        var clientAddress = context.GetClientIpAddress();

        return $"ip:{clientAddress ?? "unknown"}";
    }
}
