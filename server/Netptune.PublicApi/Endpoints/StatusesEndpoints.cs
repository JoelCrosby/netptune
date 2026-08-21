using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.Statuses.Commands;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.PublicApi.Endpoints;

public static class StatusesEndpoints
{
    public static RouteGroupBuilder MapStatusesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/statuses", GetStatuses)
            .WithSummary("List statuses")
            .WithDescription("Returns statuses in the credential's workspace, optionally filtered by entity type.")
            .RequireAuthorization(NetptunePermissions.Statuses.Read);

        group.MapPost("/statuses", CreateStatus)
            .WithSummary("Create a status")
            .WithDescription("Adds a status to the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Statuses.Manage);

        group.MapPatch("/statuses/{id:int}", UpdateStatus)
            .WithSummary("Update a status")
            .WithDescription("Updates an existing status.")
            .RequireAuthorization(NetptunePermissions.Statuses.Manage);

        group.MapDelete("/statuses/{id:int}", DeleteStatus)
            .WithSummary("Delete a status")
            .WithDescription("Deletes a status that nothing still references.")
            .RequireAuthorization(NetptunePermissions.Statuses.Manage);

        group.MapPost("/statuses/reorder", ReorderStatuses)
            .WithSummary("Reorder statuses")
            .WithDescription("Replaces the display order of the workspace's statuses with the supplied order.")
            .RequireAuthorization(NetptunePermissions.Statuses.Manage);

        return group;
    }

    private static async Task<Results<Ok<List<StatusViewModel>>, NotFound>> GetStatuses(
        IMediator mediator,
        [AsParameters] StatusFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStatusesQuery(filter), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Created<StatusViewModel>, NotFound, BadRequest<ClientResponse<StatusViewModel>>>> CreateStatus(
        IMediator mediator,
        CreateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateStatusCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/statuses/{result.Payload!.Id}", result.Payload);
    }

    private static async Task<Results<Ok<StatusViewModel>, NotFound, BadRequest<ClientResponse<StatusViewModel>>>> UpdateStatus(
        IMediator mediator,
        int id,
        UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        request = request with { Id = id };
        var result = await mediator.Send(new UpdateStatusCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> DeleteStatus(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteStatusCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> ReorderStatuses(
        IMediator mediator,
        ReorderStatusesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReorderStatusesCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }
}
