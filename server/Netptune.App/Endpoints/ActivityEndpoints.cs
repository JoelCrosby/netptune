using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Enums;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class ActivityEndpoints
{
    public static RouteGroupBuilder Map(this WebApplication app)
    {
        var group = app.MapGroup("activity")
            .RequireAuthorization();

        group.MapGet("/", HandleGet);

        return group;
    }

    public static async Task<IResult> HandleGet(IActivityService activityService, EntityType entityType, int entityId)
    {
        var result = await activityService.GetActivities(entityType, entityId);

        return Results.Ok(result);
    }
}
