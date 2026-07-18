using System.Threading.RateLimiting;

namespace Netptune.PublicApi.Configuration;

public sealed class PreAuthenticationRateLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<HttpContext> Limiter =
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));

    public ValueTask<RateLimitLease> AcquireAsync(HttpContext context)
    {
        return Limiter.AcquireAsync(context, permitCount: 1, context.RequestAborted);
    }

    public void Dispose()
    {
        Limiter.Dispose();
    }
}
