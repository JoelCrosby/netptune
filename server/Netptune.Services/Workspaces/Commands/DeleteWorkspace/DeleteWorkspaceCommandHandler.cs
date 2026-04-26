using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Workspaces.Commands.DeleteWorkspace;

public sealed class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspaceUserCache Cache;
    private readonly IActivityLogger Activity;

    public DeleteWorkspaceCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IWorkspaceUserCache cache, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Cache = cache;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Key);

        if (workspace is null) return ClientResponse.NotFound;

        var userId = Identity.GetCurrentUserId();

        Cache.Remove(new() { UserId = userId, WorkspaceKey = workspace.Slug });

        workspace.Delete(userId);

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
