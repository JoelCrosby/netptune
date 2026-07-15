using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Files;

namespace Netptune.Handlers.Storage.Queries;

public sealed record GetTaskFilesQuery(string SystemId) : IRequest<ClientResponse<IReadOnlyList<WorkspaceFileViewModel>>>;

public sealed class GetTaskFilesQueryHandler : IRequestHandler<GetTaskFilesQuery, ClientResponse<IReadOnlyList<WorkspaceFileViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public GetTaskFilesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse<IReadOnlyList<WorkspaceFileViewModel>>> Handle(GetTaskFilesQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var task = await UnitOfWork.Tasks.GetTaskInWorkspace(request.SystemId, workspaceId, cancellationToken);

        if (task is null || task.IsDeleted)
        {
            return ClientResponse<IReadOnlyList<WorkspaceFileViewModel>>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var permissions = await PermissionCache.GetUserPermissions(userId, Identity.TryGetWorkspaceKey());
        var canDeleteAny = permissions?.Role is WorkspaceRole.Owner or WorkspaceRole.Admin || permissions?.Permissions.Contains(NetptunePermissions.Files.DeleteAny) == true;
        var files = await UnitOfWork.WorkspaceFiles.GetTaskFiles(workspaceId, task.Id, userId, canDeleteAny, cancellationToken);

        return ClientResponse<IReadOnlyList<WorkspaceFileViewModel>>.Success(files);
    }
}
