using Mediator;

using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.RelationTypes.Commands;
using Netptune.Handlers.RelationTypes.Queries;

namespace Netptune.App.Endpoints;

public static class RelationTypesEndpoints
{
    public static RouteGroupBuilder MapRelationTypesEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("relation-types");

        group.MapGet("/", HandleGet).RequireAuthorization(NetptunePermissions.RelationTypes.Read);
        group.MapGet("/page", HandleGetPage).RequireAuthorization(NetptunePermissions.RelationTypes.Read);
        group.MapGet("/{id:int}/usage", HandleGetUsage).RequireAuthorization(NetptunePermissions.RelationTypes.Read);
        group.MapGet("/{id:int}/relations", HandleGetRelations).RequireAuthorization(NetptunePermissions.RelationTypes.Read);
        group.MapPost("/", HandlePost).RequireAuthorization(NetptunePermissions.RelationTypes.Manage);
        group.MapPut("/", HandlePut).RequireAuthorization(NetptunePermissions.RelationTypes.Manage);
        group.MapDelete("/{id:int}", HandleDelete).RequireAuthorization(NetptunePermissions.RelationTypes.Manage);
        group.MapPost("/reorder", HandleReorder).RequireAuthorization(NetptunePermissions.RelationTypes.Manage);
        group.MapPost("/move", HandleMove).RequireAuthorization(NetptunePermissions.RelationTypes.Manage);

        return builder;
    }

    private static async Task<IResult> HandleGet(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRelationTypesQuery(), cancellationToken);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> HandleGetPage(
        IMediator mediator,
        [AsParameters] RelationTypeFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRelationTypesPageQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetUsage(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRelationTypeUsageQuery(id), cancellationToken);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> HandleGetRelations(
        IMediator mediator,
        int id,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRelationsForTypeQuery(id, page), cancellationToken);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> HandlePost(
        IMediator mediator,
        CreateRelationTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateRelationTypeCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandlePut(
        IMediator mediator,
        UpdateRelationTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateRelationTypeCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleDelete(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteRelationTypeCommand(id), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleReorder(
        IMediator mediator,
        ReorderRelationTypesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReorderRelationTypesCommand(request), cancellationToken);

        return result.ToResult();
    }

    private static async Task<IResult> HandleMove(
        IMediator mediator,
        MoveRelationTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MoveRelationTypeCommand(request), cancellationToken);

        return result.ToResult();
    }
}
