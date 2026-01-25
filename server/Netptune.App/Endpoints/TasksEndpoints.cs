using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class TasksEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("tasks")
            .RequireAuthorization(NetptunePolicies.Workspace);

        group.MapGet("/", HandleGetTasks);
        group.MapGet("/{id}", HandleGetTask);
        group.MapGet("/detail", HandleGetTaskDetail);
        group.MapPut("/", HandlePut);
        group.MapPost("/", HandlePost);
        group.MapDelete("/", HandleDelete);
        group.MapDelete("/{id}", HandleDeleteById);
        group.MapPost("/move-task-in-group", HandleMoveTaskInGroup);
        group.MapPost("/move-tasks-to-group", HandleMoveTasksToGroup);
        group.MapPost("/reassign-tasks", HandleReassignTasks);

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
        UpdateProjectTaskRequest request)
    {
        var result = await taskService.Update(request);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePost(
        ITaskService taskService,
        AddProjectTaskRequest request)
    {
        var result = await taskService.Create(request);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(
        ITaskService taskService,
        IEnumerable<int> ids)
    {
        var result = await taskService.Delete(ids);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDeleteById(
        ITaskService taskService,
        int id)
    {
        var result = await taskService.Delete(id);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleMoveTaskInGroup(
        ITaskService taskService,
        MoveTaskInGroupRequest request)
    {
        var result = await taskService.MoveTaskInBoardGroup(request);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleMoveTasksToGroup(
        ITaskService taskService,
        MoveTasksToGroupRequest request)
    {
        var result = await taskService.MoveTasksToGroup(request);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleReassignTasks(
        ITaskService taskService,
        ReassignTasksRequest request)
    {
        var result = await taskService.ReassignTasks(request);

        return Results.Ok(result);
    }
}
