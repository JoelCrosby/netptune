namespace Netptune.App.Middleware;

public static class MiddlewareExtensions
{
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseWorkspaceValidation()
            => app.UseMiddleware<WorkspaceValidationMiddleware>();
    }
}
