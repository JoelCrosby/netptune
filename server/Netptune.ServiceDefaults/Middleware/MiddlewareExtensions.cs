using Microsoft.AspNetCore.Builder;

namespace Netptune.ServiceDefaults.Middleware;

public static class MiddlewareExtensions
{
    extension(IApplicationBuilder app)
    {
        // Stamps every response with a correlation id and shapes unhandled failures as
        // problem+json. Every host calls this so one API surface reports errors one way.
        public IApplicationBuilder UseNetptuneRequestDefaults()
        {
            app.UseMiddleware<CorrelationIdMiddleware>();

            return app.UseMiddleware<ServerErrorLoggingMiddleware>();
        }
    }
}
