using Mediator;
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
    private const long MaxFileSize = 50L * 1024 * 1024;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;

    public UploadProfilePictureCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IStorageService storage)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
    }

    public async ValueTask<ClientResponse<UploadResponse>> Handle(UploadProfilePictureCommand request, CancellationToken cancellationToken)
    {
        if (request.Length > MaxFileSize)
        {
            return ClientResponse<UploadResponse>.Failed("Request file size exceeds maximum of 50 MiB.");
        }

        var userId = Identity.GetCurrentUserId();
        var extension = Path.GetExtension(request.FileName);
        var key = Path.Join(PathConstants.ProfilePicturePath, $"{userId}-{UniqueIdBuilder.Generate(userId)}{extension}");
        var uploadOptions = new StorageUploadOptions
        {
            Name = key,
            Key = key,
            ContentType = request.ContentType,
            Access = StorageAccess.PublicRead,
        };

        var result = await Storage.UploadFileAsync(request.Content, uploadOptions, cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return ClientResponse<UploadResponse>.Failed("The profile picture could not be uploaded.");
        }

        var user = await UnitOfWork.Users.GetAsync(userId, cancellationToken: cancellationToken);

        if (user is null)
        {
            return ClientResponse<UploadResponse>.NotFound;
        }

        user.PictureUrl = result.Payload.Uri;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return result;
    }
}
