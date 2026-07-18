using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.PublicApi.Endpoints;

public static class StatusesEndpoints
{
    public static RouteGroupBuilder MapStatusesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/statuses", GetStatuses)
            .WithSummary("List statuses")
            .WithDescription("Returns statuses in the credential's workspace, optionally filtered by entity type.")
            .RequireAuthorization(NetptunePermissions.Statuses.Read);

        return group;
    }

    private static async Task<IResult> GetStatuses(
        GetStatusesQueryHandler handler,
        [AsParameters] StatusFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetStatusesQuery(filter), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
