using Netptune.App.Services;
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
        string group)
    {
        var clientId = context.Connection.Id;
        var userId = identity.GetCurrentUserId();

        await boardEventService.SubscribeAsync(group, clientId, userId, context.Response, context.RequestAborted);
    }
}
