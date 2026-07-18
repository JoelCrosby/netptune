using Netptune.PublicApi.Configuration;

namespace Netptune.PublicApi.Middleware;

public sealed class PreAuthenticationRateLimiterMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PreAuthenticationRateLimiter limiter)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await next(context);
            return;
        }

        using var lease = await limiter.AcquireAsync(context);

        if (!lease.IsAcquired)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        await next(context);
    }
}
