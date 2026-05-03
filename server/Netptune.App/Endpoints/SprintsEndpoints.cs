using Mediator;
using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Services.Sprints.Commands;
using Netptune.Services.Sprints.Queries;

namespace Netptune.App.Endpoints;

public static class SprintsEndpoints
{
    public static RouteGroupBuilder MapSprintsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("sprints");

        group.MapGet("/", HandleGetSprints).RequireAuthorization(NetptunePermissions.Sprints.Read);
        group.MapGet("/{id:int}", HandleGetSprint).RequireAuthorization(NetptunePermissions.Sprints.Read);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Sprints.Create);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.Sprints.Update);
        group.MapDelete("/{id:int}", HandleDelete).RequireAuthorization(NetptunePermissions.Sprints.Delete);
        group.MapPost("/{id:int}/start", HandleStart).RequireAuthorization(NetptunePermissions.Sprints.Update);
        group.MapPost("/{id:int}/complete", HandleComplete).RequireAuthorization(NetptunePermissions.Sprints.Update);
        group.MapPost("/{id:int}/tasks", HandleAddTasks).RequireAuthorization(NetptunePermissions.Sprints.ManageTasks);
        group.MapDelete("/{id:int}/tasks/{taskId:int}", HandleRemoveTask).RequireAuthorization(NetptunePermissions.Sprints.ManageTasks);

        return group;
    }

    public static async Task<IResult> HandleGetSprints(
        IMediator mediator,
        [AsParameters] SprintFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSprintsQuery(filter.ProjectId, filter.Status), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetSprint(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSprintQuery(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        IMediator mediator,
        AddSprintRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateSprintCommand(request), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IMediator mediator,
        UpdateSprintRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateSprintCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteSprintCommand(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleStart(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new StartSprintCommand(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleComplete(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CompleteSprintCommand(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleAddTasks(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        int id,
        AddTasksToSprintRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddTasksToSprintCommand(id, request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleRemoveTask(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        int id,
        int taskId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveTaskFromSprintCommand(id, taskId), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    private static Task BroadcastAsync(IBoardEventService boardEventService, HttpContext context)
    {
        var group = context.Request.Headers["X-Group"].ToString();
        var clientId = context.Connection.Id;

        if (string.IsNullOrEmpty(group)) return Task.CompletedTask;

        return boardEventService.BroadcastAsync(group, clientId);
    }
}
