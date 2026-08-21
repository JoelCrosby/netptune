using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Commands;
using Netptune.Handlers.Comments.Queries;
using Netptune.Handlers.Tasks.Queries;
using Netptune.PublicApi.Configuration;
using Netptune.PublicApi.Requests;

namespace Netptune.PublicApi.Endpoints;

public static class CommentsEndpoints
{
    public static RouteGroupBuilder MapCommentsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/tasks/{id:int}/comments", GetTaskComments)
            .WithSummary("List the comments on a task")
            .WithDescription("Returns a page of the comments left on a task, oldest first.")
            .RequireAuthorization(NetptunePermissions.Comments.Read);

        group.MapPost("/tasks/{id:int}/comments", CreateTaskComment)
            .WithSummary("Comment on a task")
            .WithDescription(
                "Adds a comment to a task, authored by the service account the credential belongs to. "
                + "Mentions name workspace members by their user id.")
            .RequireAuthorization(NetptunePermissions.Comments.Create)
            .Broadcasts(WorkspaceEventScopes.Comment, WorkspaceEventScopes.Task);

        group.MapPatch("/comments/{id:int}", UpdateComment)
            .WithSummary("Update a comment")
            .WithDescription("Replaces the body of a comment the credential's service account authored.")
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Comments.Create)
            .Broadcasts(WorkspaceEventScopes.Comment);

        group.MapDelete("/comments/{id:int}", DeleteComment)
            .WithSummary("Delete a comment")
            .WithDescription(
                "Deletes a comment the credential's service account authored. Deleting a comment somebody else "
                + "wrote additionally needs comments.delete_any.")
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Comments.DeleteOwn)
            .Broadcasts(WorkspaceEventScopes.Comment, WorkspaceEventScopes.Task);

        group.MapPut("/comments/{id:int}/reactions/{value}", AddCommentReaction)
            .WithSummary("React to a comment")
            .WithDescription("Adds an emoji reaction to a comment on behalf of the credential's service account.")
            .RequireAuthorization(NetptunePermissions.Comments.Create)
            .Broadcasts(WorkspaceEventScopes.Comment);

        group.MapDelete("/comments/{id:int}/reactions/{value}", RemoveCommentReaction)
            .WithSummary("Remove a reaction from a comment")
            .WithDescription("Removes an emoji reaction the credential's service account added.")
            .RequireAuthorization(NetptunePermissions.Comments.Create)
            .Broadcasts(WorkspaceEventScopes.Comment);

        return group;
    }

    private static async Task<Results<Ok<List<CommentViewModel>>, NotFound>> GetTaskComments(
        IMediator mediator,
        int id,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var systemId = await ResolveSystemId(mediator, id, cancellationToken);

        if (systemId is null)
        {
            return TypedResults.NotFound();
        }

        var result = await mediator.Send(new GetCommentsForTaskQuery(systemId, page), cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Created<CommentViewModel>, NotFound, BadRequest<ClientResponse<CommentViewModel>>>> CreateTaskComment(
        IMediator mediator,
        int id,
        PublicAddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var systemId = await ResolveSystemId(mediator, id, cancellationToken);

        if (systemId is null)
        {
            return TypedResults.NotFound();
        }

        var addRequest = request.ToRequest(systemId);
        var result = await mediator.Send(new AddCommentToTaskCommand(addRequest), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/comments/{result.Payload!.Id}", result.Payload);
    }

    private static async Task<Results<Ok<CommentViewModel>, NotFound, BadRequest<ClientResponse<CommentViewModel>>, ForbidHttpResult>> UpdateComment(
        IMediator mediator,
        int id,
        UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateCommentCommand(id, request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (result.IsForbidden)
        {
            return TypedResults.Forbid();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>, ForbidHttpResult>> DeleteComment(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteCommentCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (result.IsForbidden)
        {
            return TypedResults.Forbid();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }

    private static async Task<Results<Ok<CommentViewModel>, NotFound, BadRequest<ClientResponse<CommentViewModel>>>> AddCommentReaction(
        IMediator mediator,
        int id,
        string value,
        CancellationToken cancellationToken)
    {
        var request = new CommentReactionRequest
        {
            Value = value,
        };

        var result = await mediator.Send(new AddCommentReactionCommand(id, request), cancellationToken);

        return ToReactionResult(result);
    }

    private static async Task<Results<Ok<CommentViewModel>, NotFound, BadRequest<ClientResponse<CommentViewModel>>>> RemoveCommentReaction(
        IMediator mediator,
        int id,
        string value,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveCommentReactionCommand(id, value), cancellationToken);

        return ToReactionResult(result);
    }

    private static Results<Ok<CommentViewModel>, NotFound, BadRequest<ClientResponse<CommentViewModel>>> ToReactionResult(
        ClientResponse<CommentViewModel> result)
    {
        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
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
