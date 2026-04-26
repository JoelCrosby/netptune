using Netptune.Core.Cache;
using Netptune.Core.Services;

namespace Netptune.App.Middleware;

public class WorkspaceValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var workspaceKey = identity.TryGetWorkspaceKey();

            if (workspaceKey is not null)
            {
                var userId = identity.GetCurrentUserId();
                var permissions = await permissionCache.GetUserPermissions(userId, workspaceKey);

                if (permissions is null)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
        }

        await next(context);
    }
}
