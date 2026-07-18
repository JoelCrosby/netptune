using Mediator;

using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Handlers.Activity.Queries;

namespace Netptune.App.Endpoints;

public static class ActivityEndpoints
{
    public static RouteGroupBuilder MapActivityEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("activity");

        group.MapGet("/{entityType}/{id}", HandleGet).RequireAuthorization(NetptunePermissions.Activity.Read);

        return group;
    }

    public static async Task<IResult> HandleGet(
        IMediator mediator,
        HttpContext context,
        EntityType entityType,
        [FromRoute] int id,
        [AsParameters] CursorRequest cursor,
        CancellationToken cancellationToken)
    {
        var take = cursor.GetTake();
        var query = new GetActivitiesQuery
        {
            EntityType = entityType,
            EntityId = id,
            Take = take,
            Cursor = cursor.Cursor,
        };

        var result = await mediator.Send(query, cancellationToken);

        if (result.Payload?.Count == take && result.Payload.LastOrDefault() is { } last)
        {
            context.Response.Headers["X-Next-Cursor"] = CursorRequest.Create(last.Time, last.Id);
        }

        context.Response.Headers["X-Page-Limit"] = take.ToString();

        return Results.Ok(result);
    }
}
