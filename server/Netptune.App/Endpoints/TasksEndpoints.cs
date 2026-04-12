using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

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

    public static async Task<IResult> HandleGetTasks(
        ITaskService taskService)
    {
        var result = await taskService.GetTasks();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTask(
        ITaskService taskService,
        int id)
    {
        var result = await taskService.GetTask(id);

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetTaskDetail(
        ITaskService taskService,
        string systemId)
    {
        var result = await taskService.GetTaskDetail(systemId);

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePut(
        ITaskService taskService,
        IBoardEventService boardEventService,
        HttpContext context,
        UpdateProjectTaskRequest request)
    {
        var result = await taskService.Update(request);

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        ITaskService taskService,
        IBoardEventService boardEventService,
        HttpContext context,
        AddProjectTaskRequest request)
    {
        var result = await taskService.Create(request);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        ITaskService taskService,
        IBoardEventService boardEventService,
        HttpContext context,
        IEnumerable<int> ids)
    {
        var result = await taskService.Delete(ids);

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeleteById(
        ITaskService taskService,
        IBoardEventService boardEventService,
        HttpContext context,
        int id)
    {
        var result = await taskService.Delete(id);

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleMoveTaskInGroup(
        ITaskService taskService,
        IBoardEventService boardEventService,
        HttpContext context,
        MoveTaskInGroupRequest request)
    {
        var result = await taskService.MoveTaskInBoardGroup(request);

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleMoveTasksToGroup(
        ITaskService taskService,
        IBoardEventService boardEventService,
        HttpContext context,
        MoveTasksToGroupRequest request)
    {
        var result = await taskService.MoveTasksToGroup(request);

        if (result.IsNotFound) return Results.NotFound(result);

        await BroadcastAsync(boardEventService, context);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleReassignTasks(
        ITaskService taskService,
        IBoardEventService boardEventService,
        HttpContext context,
        ReassignTasksRequest request)
    {
        var result = await taskService.ReassignTasks(request);

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
