using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Calendar.Queries;

namespace Netptune.App.Endpoints;

public static class CalendarEndpoints
{
    public static RouteGroupBuilder MapCalendarEndpoints(this RouteGroupBuilder builder)
    {
        builder
            .MapGet("calendar/tasks", GetTasks)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        return builder;
    }

    private static async Task<IResult> GetTasks(
        IMediator mediator,
        [AsParameters] CalendarTaskFilter filter,
        CancellationToken cancellationToken)
    {
        var query = new GetCalendarTasksQuery(filter);
        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(result);
    }
}
