using Mediator;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
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
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        group.MapPatch("/tasks/{id:int}", UpdateTask)
            .WithSummary("Update a task")
            .WithDescription("Updates the supplied fields on an existing task. Tag values must already exist in the credential's workspace.")
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        return group;
    }

    private static async Task<Ok<ClientResponse<PagedResponse<TaskViewModel>>>> GetTasks(
        IMediator mediator,
        [AsParameters] PublicTaskFilter filter,
        CancellationToken cancellationToken)
    {
        var taskFilter = filter.ToTaskFilter();
        var result = await mediator.Send(new GetTasksQuery(taskFilter), cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<TaskViewModel>, NotFound>> GetTask(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Created<TaskViewModel>, BadRequest<ClientResponse<TaskViewModel>>>> CreateTask(
        IMediator mediator,
        AddProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTaskCommand(request), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/tasks/{result.Payload!.Id}", result.Payload)
            : TypedResults.BadRequest(result);
    }

    private static async Task<Results<Ok<TaskViewModel>, NotFound<ClientResponse<TaskViewModel>>, BadRequest<ClientResponse<TaskViewModel>>, ForbidHttpResult>> UpdateTask(
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
                return TypedResults.Forbid();
            }
        }

        request.Id = id;
        var result = await mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        if (result.IsNotFound) return TypedResults.NotFound(result);
        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound<ClientResponse>, BadRequest<ClientResponse>, ForbidHttpResult>> BulkUpdateTasks(
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
                return TypedResults.Forbid();
            }
        }

        var result = await mediator.Send(
            new BulkUpdateTasksCommand(request.ToRequest()),
            cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound(result);
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }
}
