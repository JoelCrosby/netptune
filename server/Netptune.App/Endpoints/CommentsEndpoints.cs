using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Comments.Commands;
using Netptune.Handlers.Comments.Queries;

namespace Netptune.App.Endpoints;

public static class CommentsEndpoints
{
    public static RouteGroupBuilder MapCommentsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("comments");

        group.MapGet("/task/{systemId}", HandleGetCommentsForTask).RequireAuthorization(NetptunePermissions.Comments.Read);
        group.MapPost("/task", HandlePostTaskComment).RequireAuthorization(NetptunePermissions.Comments.Create);
        group.MapPut("/{id:int}", HandleUpdate).RequireAuthorization(NetptunePermissions.Comments.Create);
        group.MapDelete("/{id:int}", HandleDelete).RequireAuthorization(NetptunePermissions.Comments.Read);

        return group;
    }

    public static async Task<IResult> HandleGetCommentsForTask(IMediator mediator, string systemId,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCommentsForTaskQuery(systemId, page), cancellationToken);

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePostTaskComment(IMediator mediator, AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddCommentToTaskCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(IMediator mediator, int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteCommentCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        if (result.IsForbidden)
        {
            return Results.Forbid();
        }

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleUpdate(IMediator mediator, int id, UpdateCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateCommentCommand(id, request), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        if (result.IsForbidden)
        {
            return Results.Forbid();
        }

        if (!result.IsSuccess)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }
}
