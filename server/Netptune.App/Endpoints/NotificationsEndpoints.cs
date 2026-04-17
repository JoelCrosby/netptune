using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

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

    private static async Task<IResult> HandleGet(IIdentityService identity, INetptuneUnitOfWork unitOfWork)
    {
        var userId = identity.GetCurrentUserId();
        var workspaceId = await identity.GetWorkspaceId();
        var notifications = await unitOfWork.Notifications.GetUserNotifications(userId, workspaceId);

        return Results.Ok(notifications);
    }

    private static async Task<IResult> HandleGetUnreadCount(IIdentityService identity, INetptuneUnitOfWork unitOfWork)
    {
        var userId = identity.GetCurrentUserId();
        var workspaceId = await identity.GetWorkspaceId();
        var count = await unitOfWork.Notifications.GetUnreadCount(userId, workspaceId);

        return Results.Ok(count);
    }

    private static async Task<IResult> HandleMarkRead(
        int id,
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork)
    {
        var userId = identity.GetCurrentUserId();
        await unitOfWork.Notifications.MarkAsRead(id, userId);
        await unitOfWork.CompleteAsync();

        return Results.Ok();
    }

    private static async Task<IResult> HandleMarkAllRead(
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork)
    {
        var userId = identity.GetCurrentUserId();
        var workspaceId = await identity.GetWorkspaceId();
        await unitOfWork.Notifications.MarkAllAsRead(userId, workspaceId);

        return Results.Ok();
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
