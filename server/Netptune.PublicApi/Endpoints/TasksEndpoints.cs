using Mediator;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Flags;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Flags.Commands;
using Netptune.Handlers.Flags.Queries;
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
            .WithDescription(
                "Returns a paginated list of tasks. Filter by status, priority, assignee, or tag. "
                + "Use hasTags, hasFlags and noSprint to select tasks by the presence of tags, flags or a sprint.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapGet("/tasks/archived", GetArchivedTasks)
            .WithSummary("List archived tasks")
            .WithDescription("Returns a paginated list of the deleted tasks that can still be restored.")
            .RequireAuthorization(NetptunePermissions.Tasks.Restore);

        group.MapGet("/tasks/status-breakdown", GetStatusBreakdown)
            .WithSummary("Get the task status breakdown")
            .WithDescription("Returns how many tasks in the workspace sit in each status.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapGet("/tasks/{id:int}", GetTask)
            .WithSummary("Get a task")
            .WithDescription("Returns a task by its numeric identifier.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapPost("/tasks", CreateTask)
            .WithSummary("Create a task")
            .WithDescription(
                "Creates a task in the credential's workspace. Tag values must already exist in the workspace, and "
                + "relations name an existing task by its key.")
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Tasks.Create);

        group.MapPost("/tasks/bulk-update", BulkUpdateTasks)
            .WithSummary("Bulk update tasks")
            .WithDescription(
                "Updates the supplied fields on multiple tasks in the credential's workspace. "
                + "Supply boardGroupId to move them into a board column, from GET /board-groups.")
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        group.MapPost("/tasks/restore", RestoreTasks)
            .WithSummary("Restore archived tasks")
            .WithDescription("Restores previously deleted tasks, returning them to their board.")
            .RequireAuthorization(NetptunePermissions.Tasks.Restore);

        group.MapPost("/tasks/reassign", ReassignTasks)
            .WithSummary("Reassign tasks")
            .WithDescription("Replaces the assignees on the supplied tasks with a single assignee.")
            .RequireAuthorization(NetptunePermissions.Tasks.Reassign);

        group.MapPost("/tasks/move", MoveTasks)
            .WithSummary("Move tasks between board columns")
            .WithDescription(
                "Moves the supplied tasks into a board column. Supply sortOrder to place a single task at a "
                + "position within the column rather than appending it.")
            .RequireAuthorization(NetptunePermissions.Tasks.Move);

        group.MapPatch("/tasks/{id:int}", UpdateTask)
            .WithSummary("Update a task")
            .WithDescription(
                "Updates the supplied fields on an existing task. Tag values must already exist in the credential's "
                + "workspace. Supply boardGroupId to move the task into a board column, from GET /board-groups.")
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        group.MapDelete("/tasks/{id:int}", DeleteTask)
            .WithSummary("Delete a task")
            .WithDescription("Archives a task. Archived tasks stay restorable through POST /tasks/restore.")
            .RequireAuthorization(NetptunePermissions.Tasks.Delete);

        group.MapGet("/tasks/{id:int}/flags", GetTaskFlags)
            .WithSummary("List the flags raised on a task")
            .WithDescription("Returns the automation flags raised against a task, resolved and unresolved.")
            .RequireAuthorization(NetptunePermissions.Flags.Read);

        group.MapPut("/tasks/{id:int}/flags/{flagId:int}/resolution", ResolveTaskFlag)
            .WithSummary("Resolve a flag on a task")
            .WithDescription("Records how a flag raised against a task was resolved.")
            .RequireAuthorization(NetptunePermissions.Flags.Resolve);

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

    private static async Task<Ok<ClientResponse<PagedResponse<TaskViewModel>>>> GetArchivedTasks(
        IMediator mediator,
        [AsParameters] PublicTaskFilter filter,
        CancellationToken cancellationToken)
    {
        var taskFilter = filter.ToTaskFilter();
        var result = await mediator.Send(new GetArchivedTasksQuery(taskFilter), cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<ClientResponse<TaskStatusBreakdownViewModel>>> GetStatusBreakdown(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskStatusBreakdownQuery(), cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<NoContent, NotFound<ClientResponse>, BadRequest<ClientResponse>>> RestoreTasks(
        IMediator mediator,
        PublicTaskIdsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestoreTasksCommand(request.TaskIds), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound(result);
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound<ClientResponse>, BadRequest<ClientResponse>>> ReassignTasks(
        IMediator mediator,
        ReassignTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReassignTasksCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound(result);
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound<ClientResponse>, BadRequest<ClientResponse>>> MoveTasks(
        IMediator mediator,
        PublicMoveTasksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await SendMove(mediator, request, cancellationToken);


        if (result.IsNotFound)
        {
            return TypedResults.NotFound(result);
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound<ClientResponse>, BadRequest<ClientResponse>>> DeleteTask(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound(result);
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Ok<ClientResponse<List<TaskFlagViewModel>>>> GetTaskFlags(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskFlagsQuery(id), cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<NoContent, NotFound<ClientResponse>, BadRequest<ClientResponse>>> ResolveTaskFlag(
        IMediator mediator,
        int id,
        int flagId,
        ResolveTaskFlagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ResolveTaskFlagCommand(id, flagId, request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound(result);
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async ValueTask<ClientResponse> SendMove(
        IMediator mediator,
        PublicMoveTasksRequest request,
        CancellationToken cancellationToken)
    {
        var placesOneTask = request.Position.HasValue && request.TaskIds.Count == 1;

        if (!placesOneTask)
        {
            return await mediator.Send(
                new MoveTasksToBoardGroupCommand(request.TaskIds, request.BoardGroupId),
                cancellationToken);
        }

        var task = await mediator.Send(new GetTaskQuery(request.TaskIds[0]), cancellationToken);

        if (task is null)
        {
            return ClientResponse.NotFound;
        }

        var currentBoardGroupId = task.BoardGroupId ?? request.BoardGroupId;
        var moveInGroup = request.ToMoveInGroupRequest(currentBoardGroupId);

        return await mediator.Send(new MoveTaskInBoardGroupCommand(moveInGroup), cancellationToken);
    }

    private static async Task<Results<Ok<TaskViewModel>, NotFound>> GetTask(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Created<TaskViewModel>, BadRequest<ClientResponse<TaskViewModel>>, ForbidHttpResult>> CreateTask(
        IMediator mediator,
        IAuthorizationService authorization,
        HttpContext http,
        AddProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Tags is not null)
        {
            var canAssignTags = await authorization.AuthorizeAsync(http.User, NetptunePermissions.Tags.Assign);

            if (!canAssignTags.Succeeded)
            {
                return TypedResults.Forbid();
            }
        }

        if (request.Relations is not null)
        {
            var canLinkTasks = await authorization.AuthorizeAsync(http.User, NetptunePermissions.Tasks.Update);

            if (!canLinkTasks.Succeeded)
            {
                return TypedResults.Forbid();
            }
        }

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
        PublicUpdateTaskRequest request,
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

        if (request.BoardGroupId.HasValue)
        {
            var canMoveTasks = await authorization.AuthorizeAsync(http.User, NetptunePermissions.Tasks.Move);

            if (!canMoveTasks.Succeeded)
            {
                return TypedResults.Forbid();
            }
        }

        var result = await mediator.Send(new UpdateTaskCommand(request.ToRequest(id)), cancellationToken);

        if (result.IsNotFound) return TypedResults.NotFound(result);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        if (!request.BoardGroupId.HasValue)
        {
            return TypedResults.Ok(result.Payload);
        }

        var move = await mediator.Send(
            new MoveTasksToBoardGroupCommand([id], request.BoardGroupId.Value),
            cancellationToken);

        if (!move.IsSuccess)
        {
            return MoveFailure<TaskViewModel>(move);
        }

        var moved = await mediator.Send(new GetTaskQuery(id), cancellationToken);

        return TypedResults.Ok(moved);
    }

    private static Results<Ok<TValue>, NotFound<ClientResponse<TValue>>, BadRequest<ClientResponse<TValue>>, ForbidHttpResult> MoveFailure<TValue>(
        ClientResponse move)
    {
        var message = move.Message ?? "The task could not be moved to the board group.";

        if (move.IsNotFound)
        {
            return TypedResults.NotFound(ClientResponse<TValue>.Failed(message));
        }

        return TypedResults.BadRequest(ClientResponse<TValue>.Failed(message));
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

        if (request.BoardGroupId.HasValue)
        {
            var canMoveTasks = await authorization.AuthorizeAsync(http.User, NetptunePermissions.Tasks.Move);

            if (!canMoveTasks.Succeeded)
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

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        if (!request.BoardGroupId.HasValue)
        {
            return TypedResults.NoContent();
        }

        var move = await mediator.Send(
            new MoveTasksToBoardGroupCommand(request.TaskIds, request.BoardGroupId.Value),
            cancellationToken);

        if (move.IsNotFound)
        {
            return TypedResults.NotFound(move);
        }

        return move.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(move);
    }
}
