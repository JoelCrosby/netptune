using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Tags.Commands;
using Netptune.Handlers.Tags.Queries;
using Netptune.Handlers.Tasks.Queries;
using Netptune.Api.Configuration;
using Netptune.Api.Requests;

namespace Netptune.Api.Endpoints;

public static class TagsEndpoints
{
    public static RouteGroupBuilder MapTagsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/tags", GetTags)
            .WithSummary("List tags")
            .WithDescription(
                "Returns the tags defined in the credential's workspace. Tags supplied when creating or updating a "
                + "task must already appear in this list.")
            .RequireAuthorization(NetptunePermissions.Tags.Read);

        group.MapPost("/tags", CreateTag)
            .WithSummary("Create a tag")
            .WithDescription("Adds a tag to the credential's workspace so tasks can be tagged with it.")
            .RequireAuthorization(NetptunePermissions.Tags.Create)
            .Broadcasts(WorkspaceEventScopes.Tag);

        group.MapPatch("/tags/{tag}", RenameTag)
            .WithSummary("Rename a tag")
            .WithDescription("Renames a tag across every task in the workspace that carries it.")
            .RequireAuthorization(NetptunePermissions.Tags.Update)
            .Broadcasts(WorkspaceEventScopes.Tag, WorkspaceEventScopes.Task);

        group.MapDelete("/tags/{tag}", DeleteTag)
            .WithSummary("Delete a tag")
            .WithDescription("Removes a tag from the workspace and from every task that carries it.")
            .RequireAuthorization(NetptunePermissions.Tags.Delete)
            .Broadcasts(WorkspaceEventScopes.Tag, WorkspaceEventScopes.Task);

        group.MapGet("/tasks/{id:int}/tags", GetTaskTags)
            .WithSummary("List the tags on a task")
            .WithDescription("Returns the tags currently applied to a task.")
            .RequireAuthorization(NetptunePermissions.Tags.Read);

        group.MapPut("/tasks/{id:int}/tags/{tag}", AddTagToTask)
            .WithSummary("Add a tag to a task")
            .WithDescription("Applies a tag to a task, creating the tag in the workspace when it does not exist yet.")
            .RequireAuthorization(NetptunePermissions.Tags.Assign)
            .Broadcasts(WorkspaceEventScopes.Tag, WorkspaceEventScopes.Task);

        group.MapDelete("/tasks/{id:int}/tags/{tag}", RemoveTagFromTask)
            .WithSummary("Remove a tag from a task")
            .WithDescription("Removes a tag from a task, leaving the tag in the workspace.")
            .RequireAuthorization(NetptunePermissions.Tags.Assign)
            .Broadcasts(WorkspaceEventScopes.Tag, WorkspaceEventScopes.Task);

        return group;
    }

    private static async Task<Results<Ok<List<TagViewModel>>, NotFound>> GetTags(
        IMediator mediator,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTagsForWorkspaceQuery(page), cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Created<TagViewModel>, NotFound, BadRequest<ClientResponse<TagViewModel>>>> CreateTag(
        IMediator mediator,
        AddTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTagCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/tags/{Uri.EscapeDataString(result.Payload!.Name)}", result.Payload);
    }

    private static async Task<Results<Ok<TagViewModel>, NotFound, BadRequest<ClientResponse<TagViewModel>>>> RenameTag(
        IMediator mediator,
        string tag,
        PublicRenameTagRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = new UpdateTagRequest
        {
            CurrentValue = tag,
            NewValue = request.NewValue,
        };

        var result = await mediator.Send(new UpdateTagCommand(updateRequest), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, BadRequest<ClientResponse>>> DeleteTag(
        IMediator mediator,
        string tag,
        CancellationToken cancellationToken)
    {
        var request = new DeleteTagsRequest
        {
            Tags = [tag],
        };

        var result = await mediator.Send(new DeleteTagsCommand(request), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<Ok<List<TagViewModel>>, NotFound>> GetTaskTags(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var systemId = await ResolveSystemId(mediator, id, cancellationToken);

        if (systemId is null)
        {
            return TypedResults.NotFound();
        }

        var result = await mediator.Send(new GetTagsForTaskQuery(systemId), cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<TagViewModel>, NotFound, BadRequest<ClientResponse<TagViewModel>>>> AddTagToTask(
        IMediator mediator,
        int id,
        string tag,
        CancellationToken cancellationToken)
    {
        var systemId = await ResolveSystemId(mediator, id, cancellationToken);

        if (systemId is null)
        {
            return TypedResults.NotFound();
        }

        var request = new AddTagToTaskRequest
        {
            SystemId = systemId,
            Tag = tag,
        };

        var result = await mediator.Send(new AddTagToTaskCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> RemoveTagFromTask(
        IMediator mediator,
        int id,
        string tag,
        CancellationToken cancellationToken)
    {
        var systemId = await ResolveSystemId(mediator, id, cancellationToken);

        if (systemId is null)
        {
            return TypedResults.NotFound();
        }

        var request = new DeleteTagFromTaskRequest
        {
            SystemId = systemId,
            Tag = tag,
        };

        var result = await mediator.Send(new DeleteTagFromTaskCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
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
