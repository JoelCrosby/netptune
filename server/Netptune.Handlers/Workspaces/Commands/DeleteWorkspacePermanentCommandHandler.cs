using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Cache;
using Netptune.Core.Models.Search;
using Netptune.Core.Models.Workspaces;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Workspaces.Commands;

public sealed record DeleteWorkspacePermanentCommand(string Key) : IRequest<ClientResponse>;

public sealed class DeleteWorkspacePermanentCommandHandler : IRequestHandler<DeleteWorkspacePermanentCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IWorkspaceCache WorkspaceCache;
    private readonly IWorkspaceUserCache WorkspaceUserCache;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IEventPublisher EventPublisher;
    private readonly IStorageService Storage;
    private readonly ILogger<DeleteWorkspacePermanentCommandHandler> Logger;

    public DeleteWorkspacePermanentCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IWorkspaceCache workspaceCache,
        IWorkspaceUserCache workspaceUserCache,
        IWorkspacePermissionCache permissionCache,
        IEventPublisher eventPublisher,
        IStorageService storage,
        ILogger<DeleteWorkspacePermanentCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        WorkspaceCache = workspaceCache;
        WorkspaceUserCache = workspaceUserCache;
        PermissionCache = permissionCache;
        EventPublisher = eventPublisher;
        Storage = storage;
        Logger = logger;
    }

    public async ValueTask<ClientResponse> Handle(DeleteWorkspacePermanentCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Key, cancellationToken: cancellationToken);

        if (workspace is null) return ClientResponse.NotFound;

        var workspaceId = workspace.Id;
        var workspaceSlug = workspace.Slug;

        // The teardown order lives on the repository — every workspace-scoped foreign key is
        // Restrict, so the rows have to come out children-first or the workspace row will not budge.
        var references = await UnitOfWork.Transaction(
            () => UnitOfWork.Workspaces.DeleteWorkspacePermanent(workspaceId, cancellationToken));

        // The slug is free again once this commits, and everything below is keyed by it, so a new
        // workspace with the same slug would otherwise inherit the old members' access and search hits.
        ClearCaches(workspaceSlug, references.MemberUserIds);

        await RemoveSearchDocuments(workspaceSlug, references);
        await DeleteStoredFiles(workspaceId, references.StorageKeys);

        return ClientResponse.Success;
    }

    private void ClearCaches(string workspaceSlug, List<string> memberUserIds)
    {
        WorkspaceCache.Remove(workspaceSlug);

        foreach (var userId in memberUserIds)
        {
            var key = new WorkspaceUserKey { UserId = userId, WorkspaceKey = workspaceSlug };

            WorkspaceUserCache.Remove(key);
            PermissionCache.Remove(key);
        }
    }

    private async Task RemoveSearchDocuments(string workspaceSlug, DeletedWorkspaceReferences references)
    {
        await EventPublisher.RemoveTasks(references.TaskIds, workspaceSlug);
        await EventPublisher.RemoveProjects(references.ProjectIds, workspaceSlug);
        await EventPublisher.RemoveBoards(references.BoardIds, workspaceSlug);
        await EventPublisher.RemoveSprints(references.SprintIds, workspaceSlug);
        await EventPublisher.RemovePages(references.PageIds, workspaceSlug);
    }

    private async Task DeleteStoredFiles(int workspaceId, List<string> storageKeys)
    {
        if (storageKeys.Count == 0)
        {
            return;
        }

        try
        {
            // Not the request token: the rows are already gone, so an abandoned request must not
            // abandon the only attempt to remove the objects they pointed at.
            await Storage.DeleteFilesAsync(storageKeys, CancellationToken.None);
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to delete {Count} stored files for permanently deleted workspace {WorkspaceId}",
                storageKeys.Count,
                workspaceId);
        }
    }
}
