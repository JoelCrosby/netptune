using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Files;

namespace Netptune.Handlers.Storage.Queries;

public sealed record GetWorkspaceStorageUsageQuery : IRequest<ClientResponse<WorkspaceStorageUsageViewModel>>;

public sealed class GetWorkspaceStorageUsageQueryHandler : IRequestHandler<GetWorkspaceStorageUsageQuery, ClientResponse<WorkspaceStorageUsageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceStorageUsageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<WorkspaceStorageUsageViewModel>> Handle(GetWorkspaceStorageUsageQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var usage = await UnitOfWork.Workspaces.GetStorageUsage(workspaceId, cancellationToken);

        return usage is null
            ? ClientResponse<WorkspaceStorageUsageViewModel>.NotFound
            : ClientResponse<WorkspaceStorageUsageViewModel>.Success(usage);
    }
}
