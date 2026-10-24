using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Localization;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;
using Netptune.Core.Utilities;

namespace Netptune.Handlers.Storage.Commands;

public sealed record UploadProfilePictureCommand : IRequest<ClientResponse<UploadResponse>>
{
    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }
}

public sealed class UploadProfilePictureCommandHandler : IRequestHandler<UploadProfilePictureCommand, ClientResponse<UploadResponse>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;
    private readonly ILogger<UploadProfilePictureCommandHandler> Logger;

    public UploadProfilePictureCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IStorageService storage,
        ILogger<UploadProfilePictureCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
        Logger = logger;
    }

    public async ValueTask<ClientResponse<UploadResponse>> Handle(UploadProfilePictureCommand request, CancellationToken cancellationToken)
    {
        if (request.Length <= 0)
        {
            return ClientResponse<UploadResponse>.Failed(AppText.ErrorFileEmpty);
        }

        if (request.Length > UploadLimits.ProfilePictureMaxBytes)
        {
            var limit = UploadLimits.Describe(UploadLimits.ProfilePictureMaxBytes);

            var error = Localized.Format(AppText.ErrorRequestFileSizeExceedsMaximum, limit);

            return ClientResponse<UploadResponse>.Failed(error);
        }

        if (!ImageUploadTypes.IsAllowed(request.ContentType))
        {
            var supported = ImageUploadTypes.Describe();

            var error = Localized.Format(AppText.ErrorProfilePictureMustImage, supported);

            return ClientResponse<UploadResponse>.Failed(error);
        }

        var userId = Identity.GetCurrentUserId();
        var user = await UnitOfWork.Users.GetAsync(userId, cancellationToken: cancellationToken);

        if (user is null)
        {
            return ClientResponse<UploadResponse>.NotFound;
        }

        var contentType = ImageUploadTypes.Normalize(request.ContentType);
        var extension = ImageUploadTypes.ExtensionFor(contentType);
        var key = Path.Join(
            PathConstants.ProfilePicturePath,
            $"{userId}-{UniqueIdBuilder.Generate(userId)}{extension}");
        var uploadOptions = new StorageUploadOptions
        {
            Name = key,
            Key = key,
            ContentType = contentType,
            Access = StorageAccess.PublicRead,
        };

        var result = await Storage.UploadFileAsync(request.Content, uploadOptions, cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return ClientResponse<UploadResponse>.Failed(AppText.ErrorProfilePictureCouldNotUploaded);
        }

        var previousUrl = user.PictureUrl;

        user.PictureUrl = result.Payload.Uri;

        await UnitOfWork.CompleteAsync(cancellationToken);
        await RemovePreviousPicture(previousUrl, cancellationToken);

        return result;
    }

    private async Task RemovePreviousPicture(string? previousUrl, CancellationToken cancellationToken)
    {
        var previousKey = StorageKeys.TryResolveProfilePictureKey(previousUrl);

        if (previousKey is null)
        {
            return;
        }

        try
        {
            await Storage.DeleteFileAsync(previousKey, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Failed to remove the previous profile picture {Key}", previousKey);
        }
    }
}
