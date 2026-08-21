using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Handlers.Sprints.Commands;
using Netptune.Handlers.Sprints.Queries;
using Netptune.PublicApi.Configuration;

namespace Netptune.PublicApi.Endpoints;

public static class SprintsEndpoints
{
    public static RouteGroupBuilder MapSprintsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/sprints", GetSprints)
            .WithSummary("List sprints")
            .WithDescription("Returns sprints in the credential's workspace, optionally filtered by project or status.")
            .RequireAuthorization(NetptunePermissions.Sprints.Read);

        group.MapGet("/sprints/{id:int}", GetSprint)
            .WithSummary("Get a sprint")
            .WithDescription("Returns a sprint by its numeric identifier.")
            .RequireAuthorization(NetptunePermissions.Sprints.Read);

        group.MapPost("/sprints", CreateSprint)
            .WithSummary("Create a sprint")
            .WithDescription("Creates a sprint in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Sprints.Create)
            .Broadcasts(WorkspaceEventScopes.Sprint);

        group.MapPatch("/sprints/{id:int}", UpdateSprint)
            .WithSummary("Update a sprint")
            .WithDescription("Updates the supplied fields on an existing sprint.")
            .RequireAuthorization(NetptunePermissions.Sprints.Update)
            .Broadcasts(WorkspaceEventScopes.Sprint);

        group.MapDelete("/sprints/{id:int}", DeleteSprint)
            .WithSummary("Delete a sprint")
            .WithDescription("Deletes a planning or cancelled sprint.")
            .RequireAuthorization(NetptunePermissions.Sprints.Delete)
            .Broadcasts(WorkspaceEventScopes.Sprint);

        group.MapPost("/sprints/{id:int}/tasks", AddTasks)
            .WithSummary("Add tasks to a sprint")
            .WithDescription("Adds existing tasks from the sprint's project to a planning or active sprint.")
            .RequireAuthorization(NetptunePermissions.Sprints.ManageTasks)
            .Broadcasts(WorkspaceEventScopes.Sprint, WorkspaceEventScopes.Task);

        group.MapDelete("/sprints/{id:int}/tasks/{taskId:int}", RemoveTask)
            .WithSummary("Remove a task from a sprint")
            .WithDescription("Removes an existing task from a planning or active sprint.")
            .RequireAuthorization(NetptunePermissions.Sprints.ManageTasks)
            .Broadcasts(WorkspaceEventScopes.Sprint, WorkspaceEventScopes.Task);

        return group;
    }

    private static async Task<Ok<List<SprintViewModel>>> GetSprints(
        IMediator mediator,
        [AsParameters] SprintFilter filter,
        CancellationToken cancellationToken)
    {
        var statuses = filter.Statuses.Length > 0
            ? filter.Statuses
            : filter.Status.HasValue
                ? [filter.Status.Value]
                : [];

        var result = await mediator.Send(
            new GetSprintsQuery(
                filter.ProjectId,
                statuses,
                filter.Take,
                filter.SortBy,
                filter.SortDirection),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<SprintDetailViewModel>, NotFound>> GetSprint(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSprintQuery(id), cancellationToken);
        return result.IsNotFound ? TypedResults.NotFound() : TypedResults.Ok(result.Payload);
    }

    private static async Task<Results<Created<SprintViewModel>, NotFound, BadRequest<ClientResponse<SprintViewModel>>>> CreateSprint(
        IMediator mediator,
        AddSprintRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateSprintCommand(request), cancellationToken);

        if (result.IsNotFound) return TypedResults.NotFound();
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/sprints/{result.Payload!.Id}", result.Payload)
            : TypedResults.BadRequest(result);
    }

    private static async Task<Results<Ok<SprintViewModel>, NotFound, BadRequest<ClientResponse<SprintViewModel>>>> UpdateSprint(
        IMediator mediator,
        int id,
        UpdateSprintRequest request,
        CancellationToken cancellationToken)
    {
        request = request with { Id = id };
        var result = await mediator.Send(new UpdateSprintCommand(request), cancellationToken);

        if (result.IsNotFound) return TypedResults.NotFound();
        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> DeleteSprint(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteSprintCommand(id), cancellationToken);

        if (result.IsNotFound) return TypedResults.NotFound();
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<Ok<SprintDetailViewModel>, NotFound<ClientResponse<SprintDetailViewModel>>, BadRequest<ClientResponse<SprintDetailViewModel>>>> AddTasks(
        IMediator mediator,
        int id,
        AddTasksToSprintRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddTasksToSprintCommand(id, request),
            cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound(result);
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<Ok<SprintDetailViewModel>, NotFound<ClientResponse<SprintDetailViewModel>>, BadRequest<ClientResponse<SprintDetailViewModel>>>> RemoveTask(
        IMediator mediator,
        int id,
        int taskId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RemoveTaskFromSprintCommand(id, taskId),
            cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound(result);
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }
}
