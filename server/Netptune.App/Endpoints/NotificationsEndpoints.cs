using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Services;

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

    private static async Task<IResult> HandleGet(INotificationService notifications)
    {
        var result = await notifications.GetUserNotifications();
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetUnreadCount(INotificationService notifications)
    {
        var result = await notifications.GetUnreadCount();
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleMarkRead(int id, INotificationService notifications)
    {
        var result = await notifications.MarkAsRead(id);
        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleMarkAllRead(INotificationService notifications)
    {
        var result = await notifications.MarkAllAsRead();
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
