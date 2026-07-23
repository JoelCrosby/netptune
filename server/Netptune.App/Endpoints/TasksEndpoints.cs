using Mediator;

using Microsoft.AspNetCore.Mvc;

using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Flags.Commands;
using Netptune.Handlers.Flags.Queries;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.App.Endpoints;

public static class TasksEndpoints
{
    public static RouteGroupBuilder MapTasksEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("tasks");

        group.MapGet("/", HandleGetTasks).RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapGet("/status-breakdown", HandleGetStatusBreakdown).RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapGet("/archive", HandleGetArchivedTasks).RequireAuthorization(NetptunePermissions.Tasks.Restore);
        group.MapGet("/{id}", HandleGetTask).RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapGet("/detail", HandleGetTaskDetail).RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapGet("/{taskId:int}/flags", HandleGetTaskFlags).RequireAuthorization(NetptunePermissions.Flags.Read);
        group.MapPut("/{taskId:int}/flags/{flagId:int}/resolution", HandleResolveTaskFlag)
            .RequireAuthorization(NetptunePermissions.Flags.Resolve);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.Tasks.Update);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Tasks.Create);
        group.MapDelete("/", HandleDelete).RequireAuthorization(NetptunePermissions.Tasks.Delete);
        group.MapDelete("/{id}", HandleDeleteById).RequireAuthorization(NetptunePermissions.Tasks.Delete);
        group.MapPost("/move-task-in-group", HandleMoveTaskInGroup).RequireAuthorization(NetptunePermissions.Tasks.Move);
        group.MapPost("/move-tasks-to-group", HandleMoveTasksToGroup).RequireAuthorization(NetptunePermissions.Tasks.Move);
        group.MapPost("/reassign-tasks", HandleReassignTasks).RequireAuthorization(NetptunePermissions.Tasks.Reassign);
        group.MapPost("/bulk-update", HandleBulkUpdate).RequireAuthorization(NetptunePermissions.Tasks.Update);
        group.MapPost("/restore", HandleRestoreTasks).RequireAuthorization(NetptunePermissions.Tasks.Restore);

        return group;
    }

    public static async Task<IResult> HandleGetTaskFlags(
        IMediator mediator,
        int taskId,
        CancellationToken cancellationToken)
    {
        var flags = await mediator.Send(new GetTaskFlagsQuery(taskId), cancellationToken);

        return Results.Ok(flags);
    }

    public static async Task<IResult> HandleResolveTaskFlag(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        int taskId,
        int flagId,
        ResolveTaskFlagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ResolveTaskFlagCommand(taskId, flagId, request),
            cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetArchivedTasks(
        IMediator mediator,
        [AsParameters] TaskFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetArchivedTasksQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleRestoreTasks(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        [FromBody] IEnumerable<int> ids,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestoreTasksCommand(ids), cancellationToken);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTasks(
        IMediator mediator,
        [AsParameters] TaskFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTasksQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetStatusBreakdown(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskStatusBreakdownQuery(), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTask(IMediator mediator, int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskQuery(id), cancellationToken);

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTaskDetail(IMediator mediator, string systemId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskDetailQuery(systemId), cancellationToken);

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        UpdateProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        AddProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTaskCommand(request), cancellationToken);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        [FromBody] IEnumerable<int> ids,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTasksCommand(ids), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeleteById(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskCommand(id), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleMoveTaskInGroup(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        MoveTaskInGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MoveTaskInBoardGroupCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleMoveTasksToGroup(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        MoveTasksToGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MoveTasksToGroupCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleReassignTasks(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        ReassignTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReassignTasksCommand(request), cancellationToken);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleBulkUpdate(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        BulkUpdateTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new BulkUpdateTasksCommand(request), cancellationToken);

        await boardEventService.BroadcastRequestAsync(context);

        return Results.Ok(result);
    }

}
