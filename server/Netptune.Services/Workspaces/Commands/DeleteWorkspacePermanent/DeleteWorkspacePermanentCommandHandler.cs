using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Workspaces.Commands.DeleteWorkspacePermanent;

public sealed class DeleteWorkspacePermanentCommandHandler : IRequestHandler<DeleteWorkspacePermanentCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspaceUserCache Cache;

    public DeleteWorkspacePermanentCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IWorkspaceUserCache cache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Cache = cache;
    }

    public async ValueTask<ClientResponse> Handle(DeleteWorkspacePermanentCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Key);

        if (workspace is null) return ClientResponse.NotFound;

        var userId = Identity.GetCurrentUserId();
        var workspaceId = workspace.Id;

        Cache.Remove(new() { UserId = userId, WorkspaceKey = workspace.Slug });

        await UnitOfWork.Transaction(async () =>
        {
            var u = UnitOfWork;

            var taskIds = await u.Tasks.GetAllIdsInWorkspace(workspaceId, true);
            await u.ProjectTasksInGroups.DeleteAllByTaskId(taskIds);
            await u.ProjectTaskTags.DeleteAllByTaskId(taskIds);

            await u.Tags.DeleteAllInWorkspace(workspaceId);
            await u.Comments.DeleteAllInWorkspace(workspaceId);
            await u.Tasks.DeleteAllInWorkspace(workspaceId);
            await u.BoardGroups.DeleteAllInWorkspace(workspaceId);
            await u.Boards.DeleteAllInWorkspace(workspaceId);
            await u.Projects.DeleteAllInWorkspace(workspaceId);

            await u.Workspaces.DeletePermanent(workspaceId);

            await UnitOfWork.CompleteAsync();
        });

        return ClientResponse.Success;
    }
}
