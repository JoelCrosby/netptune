using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Services.Audit.Queries.GetAuditLog;

public sealed class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, ClientResponse<AuditLogPage>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAuditLogQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AuditLogPage>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        filter.WorkspaceId = await Identity.GetWorkspaceId();

        var page = await UnitOfWork.ActivityLogs.GetAuditLog(filter);

        return page;
    }
}
