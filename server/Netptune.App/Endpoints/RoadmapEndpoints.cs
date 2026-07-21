using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Roadmap;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Handlers.Roadmap.Queries;

namespace Netptune.App.Endpoints;

public static class RoadmapEndpoints
{
    private sealed record RoadmapRequest
    {
        public DateOnly From { get; init; }

        public DateOnly To { get; init; }

        public int[]? ProjectIds { get; init; }

        public int[]? SprintIds { get; init; }

        public string? Search { get; init; }

        public string[]? Tags { get; init; }

        public int[]? StatusIds { get; init; }

        public string[]? Assignees { get; init; }

        public RoadmapFilter ToFilter() => new()
        {
            From = From,
            To = To,
            ProjectIds = ProjectIds ?? [],
            SprintIds = SprintIds ?? [],
            Search = Search,
            Tags = Tags ?? [],
            StatusIds = StatusIds ?? [],
            Assignees = Assignees ?? [],
        };
    }

    public static RouteGroupBuilder MapRoadmapEndpoints(this RouteGroupBuilder builder)
    {
        builder
            .MapGet("roadmap", GetRoadmap)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);
        builder
            .MapGet("roadmap/unscheduled-tasks", GetUnscheduledTasks)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        return builder;
    }

    private static async Task<IResult> GetUnscheduledTasks(
        IMediator mediator,
        [AsParameters] RoadmapUnscheduledTaskFilter filter,
        CancellationToken cancellationToken)
    {
        var query = new GetUnscheduledRoadmapTasksQuery(filter);
        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetRoadmap(
        IMediator mediator,
        [AsParameters] RoadmapRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetRoadmapQuery(request.ToFilter());
        var result = await mediator.Send(query, cancellationToken);

        return ToResult(result);
    }

    private static IResult ToResult<T>(ClientResponse<T> result)
    {
        if (result.IsNotFound)
        {
            return Results.NotFound();
        }

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new { message = result.Message });
        }

        return Results.Ok(result.Payload);
    }
}
