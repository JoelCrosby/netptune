using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

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

    public static async Task<IResult> HandleGetCommentsForTask(ICommentService commentService, string systemId)
    {
        var result = await commentService.GetCommentsForTask(systemId);

        if (result is null) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandlePostTaskComment(ICommentService commentService, AddCommentRequest request)
    {
        var result = await commentService.AddCommentToTask(request);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleDelete(ICommentService commentService, int id)
    {
        var result = await commentService.Delete(id);

        if (result.IsNotFound) return Results.NotFound(result);

        return Results.Ok(result);
    }
}
