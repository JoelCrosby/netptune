using Mediator;

using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Handlers.NotificationSubscriptions.Commands;
using Netptune.Handlers.NotificationSubscriptions.Queries;

namespace Netptune.App.Endpoints;

public static class NotificationSubscriptionsEndpoints
{
    public static RouteGroupBuilder MapNotificationSubscriptionsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("notification-subscriptions");

        group.MapGet("/", HandleGetAll)
            .RequireAuthorization(NetptunePermissions.Notifications.Read);

        group.MapPut("/", HandleUpsert)
            .RequireAuthorization(NetptunePermissions.Notifications.Update);

        group.MapDelete("/{id:int}", HandleDelete)
            .RequireAuthorization(NetptunePermissions.Notifications.Update);

        return group;
    }

    private static async Task<IResult> HandleGetAll(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetNotificationSubscriptionsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpsert(
        IMediator mediator,
        UpsertNotificationSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpsertNotificationSubscriptionCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleDelete(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteNotificationSubscriptionCommand(id), cancellationToken);

        return result.ToResult();
    }
}
