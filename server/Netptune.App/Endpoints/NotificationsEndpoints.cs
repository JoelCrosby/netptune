using Mediator;
using Microsoft.AspNetCore.Mvc;
using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Handlers.Notifications.Commands;
using Netptune.Handlers.Notifications.Queries;

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

        group
            .MapPut("/read", HandleMarkReadMany)
            .RequireAuthorization(NetptunePermissions.Notifications.Update);

        group
            .MapDelete("/", HandleDelete)
            .RequireAuthorization(NetptunePermissions.Notifications.Update);

        builder
            .MapGet("/hubs/notifications", HandleSse)
            .RequireAuthorization();

        return builder;
    }

    private static async Task<IResult> HandleGet(IMediator mediator, [AsParameters] PageRequest page, [AsParameters] NotificationFilter filter, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserNotificationsPagedQuery(page, filter), cancellationToken);
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

    private static async Task<IResult> HandleMarkReadMany(IMediator mediator, [FromBody] IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MarkNotificationsAsReadCommand(ids), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete(IMediator mediator, [FromBody] IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteNotificationsCommand(ids), cancellationToken);
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
