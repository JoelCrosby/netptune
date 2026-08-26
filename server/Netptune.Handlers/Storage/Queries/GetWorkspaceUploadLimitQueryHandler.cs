using Mediator;

using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Storage.Queries;

public sealed record GetWorkspaceUploadLimitQuery : IRequest<long>;

public sealed class GetWorkspaceUploadLimitQueryHandler : IRequestHandler<GetWorkspaceUploadLimitQuery, long>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceUploadLimitQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<long> Handle(GetWorkspaceUploadLimitQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var configured = await UnitOfWork.Workspaces.GetMaxUploadBytes(workspaceId, cancellationToken);

        return UploadLimits.Clamp(configured ?? UploadLimits.DefaultMaxUploadBytes);
    }
}
