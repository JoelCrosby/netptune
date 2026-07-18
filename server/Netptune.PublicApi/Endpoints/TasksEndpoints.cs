using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.PublicApi.Endpoints;

public static class TasksEndpoints
{
    public static RouteGroupBuilder MapTasksEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/tasks", GetTasks)
            .WithSummary("List tasks")
            .WithDescription("Returns a filtered, paginated list of tasks in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapGet("/tasks/{id:int}", GetTask)
            .WithSummary("Get a task")
            .WithDescription("Returns a task by its numeric identifier.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapPost("/tasks", CreateTask)
            .WithSummary("Create a task")
            .WithDescription("Creates a task in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Tasks.Create);
        group.MapPatch("/tasks/{id:int}", UpdateTask)
            .WithSummary("Update a task")
            .WithDescription("Updates the supplied fields on an existing task.")
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        return group;
    }

    private static async Task<IResult> GetTasks(
        GetTasksQueryHandler handler,
        [AsParameters] TaskFilter filter,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await handler.Handle(new GetTasksQuery(filter), cancellationToken));
    }

    private static async Task<IResult> GetTask(
        GetTaskQueryHandler handler,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetTaskQuery(id), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateTask(
        CreateTaskCommandHandler handler,
        AddProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateTaskCommand(request), cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/tasks/{result.Payload!.Id}", result.Payload)
            : Results.BadRequest(result);
    }

    private static async Task<IResult> UpdateTask(
        UpdateTaskCommandHandler handler,
        int id,
        UpdateProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        request.Id = id;
        var result = await handler.Handle(new UpdateTaskCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);
        return result.IsSuccess ? Results.Ok(result.Payload) : Results.BadRequest(result);
    }
}
