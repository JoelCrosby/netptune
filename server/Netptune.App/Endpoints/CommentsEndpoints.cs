using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Services.Comments.Commands;
using Netptune.Services.Comments.Queries;

namespace Netptune.App.Endpoints;

public static class CommentsEndpoints
{
    public static RouteGroupBuilder MapCommentsEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("comments");

        group.MapGet("/task/{systemId}", HandleGetCommentsForTask).RequireAuthorization(NetptunePermissions.Comments.Read);
        group.MapPost("/task", HandlePostTaskComment).RequireAuthorization(NetptunePermissions.Comments.Create);
        group.MapDelete("/{id}", HandleDelete).RequireAuthorization(NetptunePermissions.Comments.DeleteOwn);

        return group;
    }

    public static async Task<IResult> HandleGetCommentsForTask(IMediator mediator, string systemId)
    {
        var result = await mediator.Send(new GetCommentsForTaskQuery(systemId));

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePostTaskComment(IMediator mediator, AddCommentRequest request)
    {
        var result = await mediator.Send(new AddCommentToTaskCommand(request));

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(IMediator mediator, int id)
    {
        var result = await mediator.Send(new DeleteCommentCommand(id));

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }
}
