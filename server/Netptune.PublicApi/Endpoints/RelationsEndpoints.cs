using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Relations;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Handlers.RelationTypes.Commands;
using Netptune.Handlers.RelationTypes.Queries;
using Netptune.Handlers.Relations.Commands;
using Netptune.Handlers.Relations.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.PublicApi.Endpoints;

public static class RelationsEndpoints
{
    public static RouteGroupBuilder MapRelationsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/relation-types", GetRelationTypes)
            .WithSummary("List relation types")
            .WithDescription(
                "Returns the relation types defined in the credential's workspace. Use the returned id as "
                + "relationTypeId when linking two tasks.")
            .RequireAuthorization(NetptunePermissions.RelationTypes.Read);

        group.MapGet("/relation-types/{id:int}/relations", GetRelationsForType)
            .WithSummary("List the task links using a relation type")
            .WithDescription("Returns a page of the task links created with a relation type.")
            .RequireAuthorization(NetptunePermissions.RelationTypes.Read);

        group.MapPost("/relation-types", CreateRelationType)
            .WithSummary("Create a relation type")
            .WithDescription("Adds a relation type to the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.RelationTypes.Manage);

        group.MapPatch("/relation-types/{id:int}", UpdateRelationType)
            .WithSummary("Update a relation type")
            .WithDescription("Updates the supplied fields on an existing relation type.")
            .RequireAuthorization(NetptunePermissions.RelationTypes.Manage);

        group.MapDelete("/relation-types/{id:int}", DeleteRelationType)
            .WithSummary("Delete a relation type")
            .WithDescription("Deletes a relation type and the task links created with it.")
            .RequireAuthorization(NetptunePermissions.RelationTypes.Manage);

        group.MapPost("/relation-types/reorder", ReorderRelationTypes)
            .WithSummary("Reorder relation types")
            .WithDescription("Replaces the display order of the workspace's relation types with the supplied order.")
            .RequireAuthorization(NetptunePermissions.RelationTypes.Manage);

        group.MapGet("/tasks/{id:int}/relations", GetTaskRelations)
            .WithSummary("List the tasks linked to a task")
            .WithDescription("Returns the relations pointing at and away from a task.")
            .RequireAuthorization(NetptunePermissions.Tasks.Read);

        group.MapPost("/tasks/{id:int}/relations", CreateTaskRelation)
            .WithSummary("Link a task to another task")
            .WithDescription(
                "Links a task to another task in the same workspace, naming the related task by its key. "
                + "Set taskIsSource to false to reverse the direction of the link.")
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        group.MapDelete("/task-relations/{id:int}", DeleteTaskRelation)
            .WithSummary("Remove a link between two tasks")
            .WithDescription("Deletes a task relation by its numeric identifier.")
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        return group;
    }

    private static async Task<Results<Ok<List<RelationTypeViewModel>>, NotFound>> GetRelationTypes(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRelationTypesQuery(), cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<PagedResponse<RelationTypeRelationViewModel>>, NotFound>> GetRelationsForType(
        IMediator mediator,
        int id,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRelationsForTypeQuery(id, page), cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Created<RelationTypeViewModel>, NotFound, BadRequest<ClientResponse<RelationTypeViewModel>>>> CreateRelationType(
        IMediator mediator,
        CreateRelationTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateRelationTypeCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/relation-types/{result.Payload!.Id}", result.Payload);
    }

    private static async Task<Results<Ok<RelationTypeViewModel>, NotFound, BadRequest<ClientResponse<RelationTypeViewModel>>>> UpdateRelationType(
        IMediator mediator,
        int id,
        UpdateRelationTypeRequest request,
        CancellationToken cancellationToken)
    {
        request = request with { Id = id };
        var result = await mediator.Send(new UpdateRelationTypeCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> DeleteRelationType(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteRelationTypeCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> ReorderRelationTypes(
        IMediator mediator,
        ReorderRelationTypesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReorderRelationTypesCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<Ok<List<TaskRelationViewModel>>, NotFound>> GetTaskRelations(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var systemId = await ResolveSystemId(mediator, id, cancellationToken);

        if (systemId is null)
        {
            return TypedResults.NotFound();
        }

        var result = await mediator.Send(new GetTaskRelationsQuery(systemId), cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Created<TaskRelationViewModel>, NotFound, BadRequest<ClientResponse<TaskRelationViewModel>>>> CreateTaskRelation(
        IMediator mediator,
        int id,
        AddTaskRelationRequest request,
        CancellationToken cancellationToken)
    {
        var systemId = await ResolveSystemId(mediator, id, cancellationToken);

        if (systemId is null)
        {
            return TypedResults.NotFound();
        }

        var createRequest = ToCreateRequest(systemId, request);
        var result = await mediator.Send(new CreateTaskRelationCommand(createRequest), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/task-relations/{result.Payload!.Id}", result.Payload);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> DeleteTaskRelation(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskRelationCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static CreateTaskRelationRequest ToCreateRequest(string systemId, AddTaskRelationRequest request)
    {
        var source = request.TaskIsSource ? systemId : request.RelatedSystemId;
        var target = request.TaskIsSource ? request.RelatedSystemId : systemId;

        return new CreateTaskRelationRequest
        {
            SourceSystemId = source,
            TargetSystemId = target,
            RelationTypeId = request.RelationTypeId,
        };
    }

    private static async Task<string?> ResolveSystemId(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var task = await mediator.Send(new GetTaskQuery(id), cancellationToken);

        return task?.SystemId;
    }
}
