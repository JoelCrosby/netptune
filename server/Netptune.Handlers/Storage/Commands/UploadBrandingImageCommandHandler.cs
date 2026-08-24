using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Extensions;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Branding;

namespace Netptune.Handlers.Storage.Commands;

public sealed record BrandingImageUpload
{
    public required BrandingImageTarget Target { get; init; }

    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }

    public int? TargetId { get; init; }
}

public sealed record UploadBrandingImageCommand(BrandingImageUpload Upload) : IRequest<ClientResponse<BrandingImageViewModel>>;

internal sealed record BrandingFileReservation
{
    public required int WorkspaceId { get; init; }

    public required string UserId { get; init; }

    public required string OriginalName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }
}

public sealed class UploadBrandingImageCommandHandler : IRequestHandler<UploadBrandingImageCommand, ClientResponse<BrandingImageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;

    public UploadBrandingImageCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IStorageService storage)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
    }

    public async ValueTask<ClientResponse<BrandingImageViewModel>> Handle(UploadBrandingImageCommand request, CancellationToken cancellationToken)
    {
        var upload = request.Upload;
        var originalName = upload.FileName.SanitizeFileName();

        if (string.IsNullOrWhiteSpace(originalName))
        {
            return ClientResponse<BrandingImageViewModel>.Failed("A valid filename is required.");
        }

        if (upload.Length <= 0)
        {
            return ClientResponse<BrandingImageViewModel>.Failed("The image is empty.");
        }

        if (upload.Length > UploadLimits.BrandingImageMaxBytes)
        {
            var limit = UploadLimits.Describe(UploadLimits.BrandingImageMaxBytes);

            return ClientResponse<BrandingImageViewModel>.Failed($"The image exceeds the {limit} limit.");
        }

        if (!BrandingImageTypes.IsAllowed(upload.ContentType))
        {
            var supported = BrandingImageTypes.Describe();

            return ClientResponse<BrandingImageViewModel>.Failed($"The image must be a {supported} file.");
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var slot = await BrandingSlots.Resolve(UnitOfWork, upload.Target, upload.TargetId, workspaceId, cancellationToken);

        if (slot is null)
        {
            return ClientResponse<BrandingImageViewModel>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var reservation = new BrandingFileReservation
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            OriginalName = originalName,
            ContentType = BrandingImageTypes.Normalize(upload.ContentType),
            Length = upload.Length,
        };

        var entity = await ReserveFile(reservation, cancellationToken);

        if (entity is null)
        {
            var usage = await UnitOfWork.Workspaces.GetStorageUsage(workspaceId, cancellationToken);
            var used = usage?.UsedBytes ?? 0;
            var limit = usage?.LimitBytes ?? 0;

            return ClientResponse<BrandingImageViewModel>.Failed($"Workspace storage limit exceeded ({used} of {limit} bytes used; {upload.Length} requested).");
        }

        try
        {
            var uploadOptions = new StorageUploadOptions
            {
                Name = originalName,
                Key = entity.StorageKey,
                ContentType = entity.ContentType,
                Access = StorageAccess.Private,
            };

            var uploaded = await Storage.UploadFileAsync(upload.Content, uploadOptions, cancellationToken);

            if (!uploaded.IsSuccess)
            {
                throw new InvalidOperationException("Object storage upload failed.");
            }

            entity.Status = WorkspaceFileStatus.Ready;

            await UnitOfWork.CompleteAsync(cancellationToken);
            await slot.Assign(entity.ContentId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await BrandingFileRelease.Release(UnitOfWork, Storage, entity, userId, cancellationToken);

            return ClientResponse<BrandingImageViewModel>.Failed("The image could not be uploaded.");
        }

        await ReleaseReplacedImage(slot.CurrentFileId, workspaceId, userId, cancellationToken);

        var viewModel = new BrandingImageViewModel
        {
            FileId = entity.ContentId,
            ContentUrl = BrandingContent.Url(workspaceKey, entity.ContentId),
            SizeBytes = entity.SizeBytes,
        };

        return ClientResponse<BrandingImageViewModel>.Success(viewModel);
    }

    private async Task<WorkspaceFile?> ReserveFile(BrandingFileReservation reservation, CancellationToken cancellationToken)
    {
        WorkspaceFile? entity = null;

        var reserved = await UnitOfWork.Transaction(async () =>
        {
            var storageReserved = await UnitOfWork.Workspaces.TryReserveStorage(
                reservation.WorkspaceId,
                reservation.Length,
                cancellationToken);

            if (!storageReserved)
            {
                return false;
            }

            entity = await UnitOfWork.WorkspaceFiles.AddAsync(new WorkspaceFile
            {
                WorkspaceId = reservation.WorkspaceId,
                Purpose = WorkspaceFilePurpose.Branding,
                Status = WorkspaceFileStatus.Pending,
                OriginalName = reservation.OriginalName,
                StorageKey = $"pending/{Guid.NewGuid():N}",
                ContentType = reservation.ContentType,
                SizeBytes = reservation.Length,
                CreatedByUserId = reservation.UserId,
                OwnerId = reservation.UserId,
            }, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            entity.StorageKey = $"{PathConstants.BrandingPath(reservation.WorkspaceId)}{entity.Id}/{Guid.NewGuid():N}";

            await UnitOfWork.CompleteAsync(cancellationToken);

            return true;
        });

        return reserved ? entity : null;
    }

    private async Task ReleaseReplacedImage(string? previousFileId, int workspaceId, string userId, CancellationToken cancellationToken)
    {
        if (previousFileId is null)
        {
            return;
        }

        var previous = await UnitOfWork.WorkspaceFiles.GetByContentId(previousFileId, workspaceId, isReadonly: true, cancellationToken);

        if (previous is null)
        {
            return;
        }

        await BrandingFileRelease.Release(UnitOfWork, Storage, previous, userId, cancellationToken);
    }
}
