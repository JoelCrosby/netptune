using Mediator;
using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Services.Notifications.Commands;
using Netptune.Services.Notifications.Queries;

namespace Netptune.App.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("notifications");

        group
            .MapGet("/", HandleGet)
            .RequireAuthorization(NetptunePermissions.Notifications.Read);

        group
            .MapGet("/unread-count", HandleGetUnreadCount)
            .RequireAuthorization(NetptunePermissions.Notifications.Read);

        group
            .MapPut("/{id:int}/read", HandleMarkRead)
            .RequireAuthorization(NetptunePermissions.Notifications.Update);

        group
            .MapPut("/read-all", HandleMarkAllRead)
            .RequireAuthorization(NetptunePermissions.Notifications.Update);

        builder
            .MapGet("/hubs/notifications", HandleSse)
            .RequireAuthorization();

        return builder;
    }

    private static async Task<IResult> HandleGet(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserNotificationsQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetUnreadCount(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUnreadCountQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleMarkRead(int id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MarkAsReadCommand(id), cancellationToken);
        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleMarkAllRead(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MarkAllAsReadCommand(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task HandleSse(
        HttpContext context,
        IIdentityService identity,
        INotificationEventService notificationEventService)
    {
        var userId = identity.GetCurrentUserId();

        await notificationEventService.SubscribeAsync(userId, context.Response, context.RequestAborted);
    }
}
