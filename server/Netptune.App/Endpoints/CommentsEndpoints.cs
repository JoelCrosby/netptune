using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class CommentsEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("comments")
            .RequireAuthorization(NetptunePolicies.Workspace);

        group.MapGet("/task/{systemId}", HandleGetCommentsForTask);
        group.MapPost("/task", HandlePostTaskComment);
        group.MapDelete("/{id}", HandleDelete);

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
