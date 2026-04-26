using Mediator;
using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Services.Tasks.Commands;
using Netptune.Services.Tasks.Commands;
using Netptune.Services.Tasks.Commands;
using Netptune.Services.Tasks.Commands;
using Netptune.Services.Tasks.Commands;
using Netptune.Services.Tasks.Commands;
using Netptune.Services.Tasks.Commands;
using Netptune.Services.Tasks.Queries;
using Netptune.Services.Tasks.Queries;
using Netptune.Services.Tasks.Queries;

namespace Netptune.App.Endpoints;

public static class TasksEndpoints
{
    public static RouteGroupBuilder MapTasksEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("tasks");

        group.MapGet("/", HandleGetTasks).RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapGet("/{id}", HandleGetTask).RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapGet("/detail", HandleGetTaskDetail).RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.Tasks.Update);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Tasks.Create);
        group.MapDelete("/", HandleDelete).RequireAuthorization(NetptunePermissions.Tasks.Delete);
        group.MapDelete("/{id}", HandleDeleteById).RequireAuthorization(NetptunePermissions.Tasks.Delete);
        group.MapPost("/move-task-in-group", HandleMoveTaskInGroup).RequireAuthorization(NetptunePermissions.Tasks.Move);
        group.MapPost("/move-tasks-to-group", HandleMoveTasksToGroup).RequireAuthorization(NetptunePermissions.Tasks.Move);
        group.MapPost("/reassign-tasks", HandleReassignTasks).RequireAuthorization(NetptunePermissions.Tasks.Reassign);

        return group;
    }

    public static async Task<IResult> HandleGetTasks(IMediator mediator)
    {
        var result = await mediator.Send(new GetTasksQuery());

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTask(IMediator mediator, int id)
    {
        var result = await mediator.Send(new GetTaskQuery(id));

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTaskDetail(IMediator mediator, string systemId)
    {
        var result = await mediator.Send(new GetTaskDetailQuery(systemId));

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        UpdateProjectTaskRequest request)
    {
        var result = await mediator.Send(new UpdateTaskCommand(request));

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        AddProjectTaskRequest request)
    {
        var result = await mediator.Send(new CreateTaskCommand(request));

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        IEnumerable<int> ids)
    {
        var result = await mediator.Send(new DeleteTasksCommand(ids));

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeleteById(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        int id)
    {
        var result = await mediator.Send(new DeleteTaskCommand(id));

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleMoveTaskInGroup(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        MoveTaskInGroupRequest request)
    {
        var result = await mediator.Send(new MoveTaskInBoardGroupCommand(request));

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleMoveTasksToGroup(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        MoveTasksToGroupRequest request)
    {
        var result = await mediator.Send(new MoveTasksToGroupCommand(request));

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleReassignTasks(
        IMediator mediator,
        IBoardEventService boardEventService,
        HttpContext context,
        ReassignTasksRequest request)
    {
        var result = await mediator.Send(new ReassignTasksCommand(request));

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
