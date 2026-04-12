using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class ActivityEndpoints
{
    public static RouteGroupBuilder MapActivityEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("activity");

        group.MapGet("/{entityType}/{id}", HandleGet).RequireAuthorization(NetptunePermissions.Activity.Read);

        return group;
    }

    public static async Task<IResult> HandleGet(IActivityService activityService, EntityType entityType, [FromRoute] int id)
    {
        var result = await activityService.GetActivities(entityType, id);

        return Results.Ok(result);
    }
}
