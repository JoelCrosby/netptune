using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Storage.Commands;

public sealed record DeleteTaskFileCommand(string SystemId, int FileId) : IRequest<ClientResponse>;

public sealed class DeleteTaskFileCommandHandler : IRequestHandler<DeleteTaskFileCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IActivityLogger Activity;

    public DeleteTaskFileCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IStorageService storage, IWorkspacePermissionCache permissionCache, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
        PermissionCache = permissionCache;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTaskFileCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var task = await UnitOfWork.Tasks.GetTaskInWorkspace(request.SystemId, workspaceId, cancellationToken);

        if (task is null || task.IsDeleted)
        {
            return ClientResponse.NotFound;
        }

        var entity = await UnitOfWork.WorkspaceFiles.GetInWorkspace(request.FileId, workspaceId, isReadonly: true, cancellationToken);
        var link = await UnitOfWork.TaskFiles.GetForTask(task.Id, request.FileId, cancellationToken);

        if (entity is null || link is null)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();

        if (!await CanDelete(entity, userId))
        {
            return ClientResponse.Forbidden;
        }

        var linkCount = await UnitOfWork.TaskFiles.CountByWorkspaceFileId(entity.Id, cancellationToken);

        if (linkCount == 1)
        {
            await ReleaseFile(entity, userId, cancellationToken);
        }
        else
        {
            await UnitOfWork.TaskFiles.DeletePermanent(link.Id, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        LogDeletion(task.Id, entity, userId);

        return ClientResponse.Success;
    }

    private async Task<bool> CanDelete(WorkspaceFile entity, string userId)
    {
        var permissions = await PermissionCache.GetUserPermissions(userId, Identity.TryGetWorkspaceKey());

        if (permissions?.Role is WorkspaceRole.Owner or WorkspaceRole.Admin)
        {
            return true;
        }

        var permission = entity.CreatedByUserId == userId
            ? NetptunePermissions.Files.DeleteOwn
            : NetptunePermissions.Files.DeleteAny;

        return permissions?.Permissions.Contains(permission) == true;
    }

    private async Task ReleaseFile(WorkspaceFile entity, string userId, CancellationToken cancellationToken)
    {
        var released = await UnitOfWork.Transaction(async () =>
        {
            var marked = await UnitOfWork.WorkspaceFiles.TryMarkQuotaReleased(entity.Id, userId, cancellationToken);

            if (!marked)
            {
                return false;
            }

            await UnitOfWork.TaskFiles.DeleteByWorkspaceFileId(entity.Id, cancellationToken);
            await UnitOfWork.Workspaces.ReleaseStorage(entity.WorkspaceId, entity.SizeBytes, cancellationToken);

            return true;
        });

        if (!released)
        {
            return;
        }

        try
        {
            await Storage.DeleteFileAsync(entity.StorageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The reconciliation job retries physical deletion.
        }
    }

    private void LogDeletion(int taskId, WorkspaceFile entity, string userId)
    {
        Activity.LogWith<FileActivityMeta>(options =>
        {
            options.EntityId = taskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.RemoveFile;
            options.Meta = new FileActivityMeta
            {
                WorkspaceFileId = entity.Id,
                FileName = entity.OriginalName,
                SizeBytes = entity.SizeBytes,
                ContentType = entity.ContentType,
                UploaderUserId = entity.CreatedByUserId ?? userId,
            };
        });
    }
}
