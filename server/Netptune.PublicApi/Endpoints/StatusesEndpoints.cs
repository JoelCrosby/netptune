using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Statuses;
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

    private static async Task<Results<Ok<List<StatusViewModel>>, NotFound>> GetStatuses(
        IMediator mediator,
        [AsParameters] StatusFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStatusesQuery(filter), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
