using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Extensions;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Storage.Commands;

public sealed record UploadStorageMediaCommand : IRequest<ClientResponse<UploadResponse>>
{
    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }
}

public sealed class UploadStorageMediaCommandHandler : IRequestHandler<UploadStorageMediaCommand, ClientResponse<UploadResponse>>
{
    private const long MaxFileSize = 50L * 1024 * 1024;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;

    public UploadStorageMediaCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IStorageService storage)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
    }

    public async ValueTask<ClientResponse<UploadResponse>> Handle(UploadStorageMediaCommand request, CancellationToken cancellationToken)
    {
        var originalName = request.FileName.SanitizeFileName();

        if (string.IsNullOrWhiteSpace(originalName))
        {
            return ClientResponse<UploadResponse>.Failed("A valid filename is required.");
        }

        if (request.Length <= 0)
        {
            return ClientResponse<UploadResponse>.Failed("The file is empty.");
        }

        if (request.Length > MaxFileSize)
        {
            return ClientResponse<UploadResponse>.Failed("The file exceeds the 50 MiB limit.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();

        WorkspaceFile? entity = null;

        var reserved = await UnitOfWork.Transaction(async () =>
        {
            var storageReserved = await UnitOfWork.Workspaces.TryReserveStorage(workspaceId, request.Length, cancellationToken);

            if (!storageReserved)
            {
                return false;
            }

            entity = await UnitOfWork.WorkspaceFiles.AddAsync(new WorkspaceFile
            {
                WorkspaceId = workspaceId,
                Purpose = WorkspaceFilePurpose.InlineMedia,
                Status = WorkspaceFileStatus.Pending,
                OriginalName = originalName,
                StorageKey = $"pending/{Guid.NewGuid():N}",
                ContentType = NormalizeContentType(request.ContentType),
                SizeBytes = request.Length,
                CreatedByUserId = userId,
                OwnerId = userId,
            }, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            entity.StorageKey = $"workspace/{workspaceId}/files/{entity.Id}/{Guid.NewGuid():N}";

            await UnitOfWork.CompleteAsync(cancellationToken);

            return true;
        });

        if (!reserved || entity is null)
        {
            var usage = await UnitOfWork.Workspaces.GetStorageUsage(workspaceId, cancellationToken);

            return ClientResponse<UploadResponse>.Failed($"Workspace storage limit exceeded ({usage?.UsedBytes ?? 0} of {usage?.LimitBytes ?? 0} bytes used; {request.Length} requested).");
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

            var uploaded = await Storage.UploadFileAsync(request.Content, uploadOptions, cancellationToken);

            if (!uploaded.IsSuccess)
            {
                throw new InvalidOperationException("Object storage upload failed.");
            }

            entity.Status = WorkspaceFileStatus.Ready;

            await UnitOfWork.CompleteAsync(cancellationToken);

            var file = await UnitOfWork.WorkspaceFiles.GetViewModel(entity.Id, userId, true, cancellationToken);

            if (file is null)
            {
                return ClientResponse<UploadResponse>.Failed("The uploaded file could not be loaded.");
            }

            return ClientResponse<UploadResponse>.Success(new UploadResponse
            {
                Name = file.OriginalName,
                Key = file.Id.ToString(),
                Path = file.ContentUrl,
                Uri = file.ContentUrl + "?disposition=inline",
                Size = file.SizeBytes,
            });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await ReleaseFile(entity, userId, cancellationToken);

            return ClientResponse<UploadResponse>.Failed("The upload could not be completed.");
        }
    }

    private async Task ReleaseFile(WorkspaceFile entity, string userId, CancellationToken cancellationToken)
    {
        await UnitOfWork.Transaction(async () =>
        {
            var released = await UnitOfWork.WorkspaceFiles.TryMarkQuotaReleased(entity.Id, userId, cancellationToken);

            if (!released)
            {
                return;
            }

            await UnitOfWork.TaskFiles.DeleteByWorkspaceFileId(entity.Id, cancellationToken);
            await UnitOfWork.Workspaces.ReleaseStorage(entity.WorkspaceId, entity.SizeBytes, cancellationToken);
        });

        try
        {
            await Storage.DeleteFileAsync(entity.StorageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The reconciliation job retries physical deletion.
        }
    }

    private static string NormalizeContentType(string contentType)
    {
        return string.IsNullOrWhiteSpace(contentType) || contentType.Length > 255
            ? "application/octet-stream"
            : contentType.ToLowerInvariant();
    }
}
