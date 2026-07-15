using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Files;

namespace Netptune.Handlers.Storage.Queries;

public sealed record GetWorkspaceFilesQuery(WorkspaceFileFilter Filter) : IRequest<ClientResponse<PagedResponse<WorkspaceFileViewModel>>>;

public sealed class GetWorkspaceFilesQueryHandler : IRequestHandler<GetWorkspaceFilesQuery, ClientResponse<PagedResponse<WorkspaceFileViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public GetWorkspaceFilesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse<PagedResponse<WorkspaceFileViewModel>>> Handle(GetWorkspaceFilesQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var permissions = await PermissionCache.GetUserPermissions(userId, Identity.TryGetWorkspaceKey());
        var canDeleteAny = permissions?.Role is WorkspaceRole.Owner or WorkspaceRole.Admin || permissions?.Permissions.Contains(NetptunePermissions.Files.DeleteAny) == true;
        var files = await UnitOfWork.WorkspaceFiles.GetWorkspaceFiles(workspaceId, userId, canDeleteAny, request.Filter, cancellationToken);

        return ClientResponse<PagedResponse<WorkspaceFileViewModel>>.Success(files);
    }
}
