using Mediator;

using Microsoft.AspNetCore.Authorization;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Handlers.Tasks.Queries;
using Netptune.PublicApi.Requests;

namespace Netptune.PublicApi.Endpoints;

public static class TasksEndpoints
{
    public static RouteGroupBuilder MapTasksEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/tasks", GetTasks)
            .WithSummary("List tasks")
            .WithDescription("Returns a paginated list of tasks. Filter by status, priority, assignee, or tag.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapGet("/tasks/{id:int}", GetTask)
            .WithSummary("Get a task")
            .WithDescription("Returns a task by its numeric identifier.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);
        group.MapPost("/tasks", CreateTask)
            .WithSummary("Create a task")
            .WithDescription("Creates a task in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Tasks.Create);
        group.MapPost("/tasks/bulk-update", BulkUpdateTasks)
            .WithSummary("Bulk update tasks")
            .WithDescription("Updates the supplied fields on multiple tasks in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Tasks.Update);
        group.MapPatch("/tasks/{id:int}", UpdateTask)
            .WithSummary("Update a task")
            .WithDescription("Updates the supplied fields on an existing task. Tag values must already exist in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        return group;
    }

    private static async Task<IResult> GetTasks(
        IMediator mediator,
        [AsParameters] PublicTaskFilter filter,
        CancellationToken cancellationToken)
    {
        var taskFilter = filter.ToTaskFilter();
        var result = await mediator.Send(new GetTasksQuery(taskFilter), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetTask(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskQuery(id), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateTask(
        IMediator mediator,
        AddProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTaskCommand(request), cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/tasks/{result.Payload!.Id}", result.Payload)
            : Results.BadRequest(result);
    }

    private static async Task<IResult> UpdateTask(
        IMediator mediator,
        IAuthorizationService authorization,
        HttpContext http,
        int id,
        UpdateProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Tags is not null)
        {
            var canAssignTags = await authorization.AuthorizeAsync(
                http.User,
                NetptunePermissions.Tags.Assign);

            if (!canAssignTags.Succeeded)
            {
                return Results.Forbid();
            }
        }

        request.Id = id;
        var result = await mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);
        return result.IsSuccess ? Results.Ok(result.Payload) : Results.BadRequest(result);
    }

    private static async Task<IResult> BulkUpdateTasks(
        IMediator mediator,
        IAuthorizationService authorization,
        HttpContext http,
        PublicBulkUpdateTasksRequest request,
        CancellationToken cancellationToken)
    {
        var changesSprintMembership = request.SprintId.HasValue || request.ClearSprint;

        if (changesSprintMembership)
        {
            var canManageSprintTasks = await authorization.AuthorizeAsync(
                http.User,
                NetptunePermissions.Sprints.ManageTasks);

            if (!canManageSprintTasks.Succeeded)
            {
                return Results.Forbid();
            }
        }

        var result = await mediator.Send(
            new BulkUpdateTasksCommand(request.ToRequest()),
            cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result);
    }
}
