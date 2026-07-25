using System.Threading.RateLimiting;

using Netptune.App.Utility;

namespace Netptune.App.Configuration;

public static class RateLimiterConfiguration
{
    private const int DefaultApiPermitLimit = 300;

    public static IServiceCollection AddNetptuneRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Every request from one user shares a partition. Integration suites drive hundreds of
        // requests as a single seeded user, so they raise this rather than trip the production limit.
        var apiPermitLimit = configuration.GetValue("RateLimiting:ApiPermitLimit", DefaultApiPermitLimit);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("auth", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.GetRemoteIpAddress() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            options.AddPolicy("register", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.GetRemoteIpAddress() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        SegmentsPerWindow = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            options.AddPolicy("api", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.GetRateLimitPartitionKey(),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = apiPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10,
                    }));

            options.AddPolicy("import-export", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.GetRateLimitPartitionKey(),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2,
                    }));
        });

        return services;
    }
}
