using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Handlers.Audit.Queries;

public sealed record GetAuditLogDetailQuery(long Id) : IRequest<ClientResponse<AuditLogDetailViewModel>>;

public sealed class GetAuditLogDetailQueryHandler
    : IRequestHandler<GetAuditLogDetailQuery, ClientResponse<AuditLogDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAuditLogDetailQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AuditLogDetailViewModel>> Handle(
        GetAuditLogDetailQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var detail = await UnitOfWork.EventRecords.GetAuditLogDetail(workspaceId, request.Id, cancellationToken);

        if (detail is null)
        {
            return ClientResponse<AuditLogDetailViewModel>.NotFound;
        }

        return detail;
    }
}
