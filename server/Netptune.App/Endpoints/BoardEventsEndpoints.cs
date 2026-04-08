using Netptune.App.Services;

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
        string group)
    {
        var clientId = context.Connection.Id;

        await boardEventService.SubscribeAsync(group, clientId, context.Response, context.RequestAborted);
    }
}
