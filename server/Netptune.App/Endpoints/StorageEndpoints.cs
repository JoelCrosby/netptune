using Mediator;
using Microsoft.AspNetCore.Mvc;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Storage.Commands;
using Netptune.Handlers.Storage.Queries;

namespace Netptune.App.Endpoints;

public static class StorageEndpoints
{
    public static RouteGroupBuilder MapStorageEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("storage");

        group.MapPost("/profile-picture", HandleUploadProfilePicture)
            .RequireAuthorization(NetptunePermissions.Storage.UploadProfilePicture);
        group.MapPost("/media", HandleUploadMedia)
            .RequireAuthorization(NetptunePermissions.Storage.UploadMedia);
        group.MapGet("/usage", HandleGetUsage)
            .RequireAuthorization(NetptunePermissions.Storage.Read);
        group.MapGet("/files", HandleGetFiles)
            .RequireAuthorization(NetptunePermissions.Storage.Read);
        group.MapDelete("/files/{id:int}", HandleDeleteFile)
            .RequireAuthorization(NetptunePermissions.Storage.Manage);

        return group;
    }

    public static async Task<IResult> HandleUploadProfilePicture(IMediator mediator, HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Multipart form data is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();

        if (file is null)
        {
            return Results.BadRequest("A file is required.");
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadProfilePictureCommand
        {
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
        };
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsNotFound)
        {
            return Results.NotFound(result);
        }

        return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
    }

    public static async Task<IResult> HandleUploadMedia(IMediator mediator, HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Multipart form data is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();

        if (file is null)
        {
            return Results.BadRequest("A file is required.");
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadStorageMediaCommand
        {
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
        };
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
    }

    private static async Task<IResult> HandleGetUsage(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceStorageUsageQuery(), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.Ok(result);
    }

    private static async Task<IResult> HandleGetFiles(IMediator mediator, [AsParameters] WorkspaceFileFilter filter, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceFilesQuery(filter), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteFile(IMediator mediator, int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteWorkspaceFileCommand(id), cancellationToken);

        return result.IsNotFound ? Results.NotFound(result) : Results.NoContent();
    }
}
