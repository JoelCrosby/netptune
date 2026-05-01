using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.Utilities;
using Netptune.Services.Users.Commands;

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

        return group;
    }

    public static async Task<IResult> HandleUploadProfilePicture(
        IStorageService storageService,
        IIdentityService identity,
        IMediator mediator,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var file = request.Form.Files.FirstOrDefault();

        if (file is null)
        {
            return Results.BadRequest("Import File must be provided. Only one file can be uploaded at a time.");
        }

        if (file.Length > 50 * 1024 * 1024)
        {
            return Results.BadRequest("Request file size exceeds maximum of 50MB.");
        }

        var userId = identity.GetCurrentUserId();
        var extension = Path.GetExtension(file.FileName);
        var key = Path.Join(PathConstants.ProfilePicturePath, $"{userId}-{UniqueIdBuilder.Generate(userId)}{extension}");

        var fileStream = file.OpenReadStream();

        var result = await storageService.UploadFileAsync(fileStream, key, key);

        if (!result.IsSuccess || result.Payload is null)
        {
            return Results.BadRequest();
        }

        await mediator.Send(new UpdateUserCommand(new()
        {
            Id = userId,
            PictureUrl = result.Payload.Uri,
        }), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleUploadMedia(
        IStorageService storageService,
        IIdentityService identity,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var workspaceKey = identity.GetWorkspaceKey();
        var file = request.Form.Files[0];

        if (file.Length > 50 * 1024 * 1024)
        {
            return Results.BadRequest("Request file size exceeds maximum of 50MB.");
        }

        var userId = identity.GetCurrentUserId();
        var extension = Path.GetExtension(file.FileName);
        var key = Path.Join(PathConstants.MediaPath(workspaceKey), $"{UniqueIdBuilder.Generate(userId)}{extension}");

        var fileStream = file.OpenReadStream();

        var result = await storageService.UploadFileAsync(fileStream, file.FileName, key);

        return Results.Ok(result);
    }
}
