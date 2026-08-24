using Mediator;

using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Handlers.TaskViews.Commands;
using Netptune.Handlers.TaskViews.Queries;
using Netptune.Query.Views;

namespace Netptune.App.Endpoints;

public static class TaskViewEndpoints
{
    public static RouteGroupBuilder MapTaskViewEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("task-views");

        group.MapGet("/fields", HandleGetFields)
            .RequireAuthorization(NetptunePermissions.TaskViews.Read);

        group.MapGet("/", HandleGetAll)
            .RequireAuthorization(NetptunePermissions.TaskViews.Read);

        group.MapGet("/{slug}", HandleGet)
            .RequireAuthorization(NetptunePermissions.TaskViews.Read);

        // Running a query answers 200 with any validation errors in the payload rather than a 4xx: a
        // half-built condition is an ordinary editing state, and a view whose status was deleted still
        // renders its remaining conditions.
        group.MapGet("/{slug}/tasks", HandleGetTasks)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapPost("/preview", HandlePreview)
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapPost("/", HandleCreate)
            .RequireAuthorization(NetptunePermissions.TaskViews.Create);

        group.MapPut("/", HandleUpdate)
            .RequireAuthorization(NetptunePermissions.TaskViews.Update);

        group.MapDelete("/{slug}", HandleDelete)
            .RequireAuthorization(NetptunePermissions.TaskViews.Delete);

        return group;
    }

    private static async Task<IResult> HandleGetFields(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetQueryFieldsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetAll(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskViewsQuery(), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGet(IMediator mediator, string slug, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskViewQuery(slug), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetTasks(
        IMediator mediator,
        string slug,
        [AsParameters] GetTaskViewTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskViewTasksQuery(slug, request), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePreview(
        IMediator mediator,
        TaskViewQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PreviewTaskQueryQuery(request), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleCreate(
        IMediator mediator,
        SaveTaskViewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SaveTaskViewCommand(request with { Id = null }), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleUpdate(
        IMediator mediator,
        SaveTaskViewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SaveTaskViewCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleDelete(IMediator mediator, string slug, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskViewCommand(slug), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        if (result.IsForbidden)
        {
            return Results.Forbid();
        }

        return Results.Ok(result);
    }
}
