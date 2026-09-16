using System.Threading.RateLimiting;

using Netptune.App.Utility;

namespace Netptune.App.Configuration;

public static class RateLimiterConfiguration
{
    public const string ApiPolicyName = "api";

    public const string AuthPolicyName = "auth";

    public const string RegisterPolicyName = "register";

    // Separate from AuthPolicyName because refresh is the one anonymous auth route legitimate clients
    // call unprompted. A shared office address can burst well past the sign-in budget at the start of
    // a day without anything being wrong.
    public const string RefreshPolicyName = "refresh";

    public const string AiPolicyName = "ai";

    public const string TransferPolicyName = "import-export";

    private const int DefaultApiPermitLimit = 300;

    private const int DefaultAiPermitLimit = 20;

    private const int DefaultAuthPermitLimit = 10;

    private const int DefaultRegisterPermitLimit = 5;

    private const int DefaultRefreshPermitLimit = 60;

    public static IServiceCollection AddNetptuneRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiPermitLimit = configuration.GetValue("RateLimiting:ApiPermitLimit", DefaultApiPermitLimit);
        var aiPermitLimit = configuration.GetValue("RateLimiting:AiPermitLimit", DefaultAiPermitLimit);
        var authPermitLimit = configuration.GetValue("RateLimiting:AuthPermitLimit", DefaultAuthPermitLimit);
        var registerPermitLimit = configuration.GetValue("RateLimiting:RegisterPermitLimit", DefaultRegisterPermitLimit);
        var refreshPermitLimit = configuration.GetValue("RateLimiting:RefreshPermitLimit", DefaultRefreshPermitLimit);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthPolicyName, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.GetRemoteIpAddress() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RegisterPolicyName, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.GetRemoteIpAddress() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = registerPermitLimit,
                        Window = TimeSpan.FromMinutes(10),
                        SegmentsPerWindow = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RefreshPolicyName, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.GetRemoteIpAddress() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = refreshPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            options.AddPolicy(ApiPolicyName, context =>
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

            // Deliberately narrow — this budget is for work that reads a file or scans the workspace, not
            // for the reads that poll it. Applying it to a listing endpoint starves the page watching a job.
            options.AddPolicy(TransferPolicyName, context =>
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

            options.AddPolicy(AiPolicyName, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.GetRateLimitPartitionKey(),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = aiPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));
        });

        return services;
    }
}
