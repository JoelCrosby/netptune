using Netptune.Transfer.Repositories;
using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record DeleteExportDefinitionCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteExportDefinitionCommandHandler : IRequestHandler<DeleteExportDefinitionCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IExportDefinitionRepository ExportDefinitions;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public DeleteExportDefinitionCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity,
        IExportDefinitionRepository exportDefinitions, IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ExportDefinitions = exportDefinitions;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse> Handle(DeleteExportDefinitionCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var definition = await ExportDefinitions.GetInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (definition is null)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var isWorkspaceWide = definition.IsShared || definition.OwnerId != userId;

        if (isWorkspaceWide)
        {
            var workspaceKey = Identity.TryGetWorkspaceKey();
            var canManage = await ExportDefinitionPermissions.CanManage(PermissionCache, userId, workspaceKey);

            if (!canManage)
            {
                return ClientResponse.Forbidden;
            }
        }

        definition.Delete(userId);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
