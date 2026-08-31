using Mediator;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Netptune.App.Configuration;
using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Realtime;
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
            .RequireAuthorization(NetptunePermissions.Flags.Resolve)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.Tasks.Update)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.Tasks.Create)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapDelete("/", HandleDelete).RequireAuthorization(NetptunePermissions.Tasks.Delete)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapDelete("/{id}", HandleDeleteById).RequireAuthorization(NetptunePermissions.Tasks.Delete)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapPost("/move-task-in-group", HandleMoveTaskInGroup).RequireAuthorization(NetptunePermissions.Tasks.Move)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapPost("/move-tasks-to-group", HandleMoveTasksToGroup)
            .RequireAuthorization(NetptunePermissions.Tasks.Move)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapPost("/{taskId:int}/boards", HandleAddTaskToBoard).RequireAuthorization(NetptunePermissions.Tasks.Move)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapDelete("/{taskId:int}/boards/{boardId:int}", HandleRemoveTaskFromBoard)
            .RequireAuthorization(NetptunePermissions.Tasks.Move)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapPost("/reassign-tasks", HandleReassignTasks).RequireAuthorization(NetptunePermissions.Tasks.Reassign)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapPost("/bulk-update", HandleBulkUpdate).RequireAuthorization(NetptunePermissions.Tasks.Update)
            .Broadcasts(WorkspaceEventScopes.Task);
        group.MapPost("/restore", HandleRestoreTasks).RequireAuthorization(NetptunePermissions.Tasks.Restore)
            .Broadcasts(WorkspaceEventScopes.Task);

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
        int taskId,
        int flagId,
        ResolveTaskFlagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ResolveTaskFlagCommand(taskId, flagId, request),
            cancellationToken);

        return result.ToResult();
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
        [FromBody] IEnumerable<int> ids,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestoreTasksCommand(ids), cancellationToken);

        return result.ToResult();
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
        UpdateProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandlePost(
        IMediator mediator,
        IAuthorizationService authorization,
        HttpContext context,
        AddProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Tags is not null)
        {
            var canAssignTags = await authorization.AuthorizeAsync(context.User, NetptunePermissions.Tags.Assign);

            if (!canAssignTags.Succeeded)
            {
                return Results.Forbid();
            }
        }

        if (request.Relations is not null)
        {
            var canLinkTasks = await authorization.AuthorizeAsync(context.User, NetptunePermissions.Tasks.Update);

            if (!canLinkTasks.Succeeded)
            {
                return Results.Forbid();
            }
        }

        var result = await mediator.Send(new CreateTaskCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleDelete(
        IMediator mediator,
        [FromBody] IEnumerable<int> ids,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTasksCommand(ids), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleDeleteById(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskCommand(id), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleMoveTaskInGroup(
        IMediator mediator,
        MoveTaskInGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MoveTaskInBoardGroupCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleMoveTasksToGroup(
        IMediator mediator,
        MoveTasksToGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MoveTasksToGroupCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleAddTaskToBoard(
        IMediator mediator,
        int taskId,
        AddTaskToBoardRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddTaskToBoardCommand(taskId, request.BoardId, request.BoardGroupId);
        var result = await mediator.Send(command, cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleRemoveTaskFromBoard(
        IMediator mediator,
        int taskId,
        int boardId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveTaskFromBoardCommand(taskId, boardId), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleReassignTasks(
        IMediator mediator,
        ReassignTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReassignTasksCommand(request), cancellationToken);

        return result.ToResult();
    }

    public static async Task<IResult> HandleBulkUpdate(
        IMediator mediator,
        BulkUpdateTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new BulkUpdateTasksCommand(request), cancellationToken);

        return result.ToResult();
    }

}
