using Netptune.App.Services;
using Netptune.Core.Cache;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class BoardEventsEndpoints
{
    public static IEndpointRouteBuilder MapBoardEventsEndpoints(this IEndpointRouteBuilder builder)
    {
        builder
            .MapGet("/hubs/board-events", HandleGet)
            .RequireAuthorization();

        return builder;
    }

    public static async Task HandleGet(
        HttpContext context,
        IBoardEventService boardEventService,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache,
        string workspace,
        string group,
        string clientId)
    {
        var userId = identity.GetCurrentUserId();
        var permissions = await permissionCache.GetUserPermissions(userId, workspace);

        if (permissions is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var subscription = new RealtimeSubscription
        {
            Workspace = workspace,
            Group = group,
            SourceClientId = clientId,
            ConnectionId = context.Connection.Id,
            UserId = userId,
        };

        await boardEventService.SubscribeAsync(subscription, context.Response, context.RequestAborted);
    }
}
