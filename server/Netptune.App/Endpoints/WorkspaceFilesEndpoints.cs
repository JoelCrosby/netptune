using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netptune.App.Services;
using Netptune.Core.Authorization;
using Netptune.Handlers.Storage.Commands;
using Netptune.Handlers.Storage.Queries;

namespace Netptune.App.Endpoints;

public static class WorkspaceFilesEndpoints
{
    private const long MaxFileSize = 50L * 1024 * 1024;
    private const long MaxRequestSize = MaxFileSize + 1024 * 1024;

    public static IEndpointRouteBuilder MapWorkspaceFilesEndpoints(this IEndpointRouteBuilder builder)
    {
        var tasks = builder.MapGroup("tasks");

        tasks
            .MapGet("/{systemId}/files", GetTaskFiles)
            .RequireAuthorization(NetptunePermissions.Tasks.Read, NetptunePermissions.Files.Read);
        tasks
            .MapPost("/{systemId}/files", UploadTaskFile)
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestSize))
            .RequireAuthorization(NetptunePermissions.Tasks.Update, NetptunePermissions.Files.Upload);
        tasks
            .MapDelete("/{systemId}/files/{fileId:int}", DeleteTaskFile)
            .RequireAuthorization(NetptunePermissions.Tasks.Update);

        builder
            .MapGet("workspaces/{workspaceKey}/files/{contentId}/content", GetContent)
            .RequireAuthorization(NetptunePermissions.Files.Read);

        return builder;
    }

    private static async Task<IResult> GetTaskFiles(string systemId, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskFilesQuery(systemId), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> UploadTaskFile(string systemId, HttpRequest request, IMediator mediator, IBoardEventService boardEvents, HttpContext http, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Multipart form data is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);

        if (form.Files.Count != 1)
        {
            return Results.BadRequest("Exactly one file is required.");
        }

        var file = form.Files.Single();

        await using var stream = file.OpenReadStream();

        var upload = new TaskFileUpload
        {
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
        };

        var result = await mediator.Send(new UploadTaskFileCommand(systemId, upload), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        await boardEvents.BroadcastRequestAsync(http);

        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteTaskFile(string systemId, int fileId, IMediator mediator, IBoardEventService boardEvents, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskFileCommand(systemId, fileId), cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound();
        }

        if (result.IsForbidden)
        {
            return Results.Forbid();
        }

        await boardEvents.BroadcastRequestAsync(http);

        return Results.NoContent();
    }

    private static async Task<IResult> GetContent(string workspaceKey, string contentId, string? disposition, IMediator mediator, IAuthorizationService authorization, HttpContext http, CancellationToken cancellationToken)
    {
        var canReadTasks = (await authorization.AuthorizeAsync(http.User, NetptunePermissions.Tasks.Read)).Succeeded;
        var query = new GetWorkspaceFileContentQuery
        {
            ContentId = contentId,
            Disposition = disposition,
            CanReadTasks = canReadTasks,
        };
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound();
        }

        if (result.IsForbidden)
        {
            return Results.Forbid();
        }

        http.Response.Headers.XContentTypeOptions = "nosniff";

        return Results.Redirect(result.Payload!.ToString());
    }

}
