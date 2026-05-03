namespace Netptune.App.Middleware;

public static class MiddlewareExtensions
{
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseCorrelationId()
            => app.UseMiddleware<CorrelationIdMiddleware>();

        public IApplicationBuilder UseServerErrorLogging()
            => app.UseMiddleware<ServerErrorLoggingMiddleware>();

        public IApplicationBuilder UseWorkspaceValidation()
            => app.UseMiddleware<WorkspaceValidationMiddleware>();
    }
}
