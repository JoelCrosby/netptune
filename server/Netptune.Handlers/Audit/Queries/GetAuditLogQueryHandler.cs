using Mediator;

using Netptune.Core.Models.Audit;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Handlers.Audit.Queries;

public sealed record GetAuditLogQuery(AuditLogFilter Filter) : IRequest<ClientResponse<PagedResponse<AuditLogViewModel>>>;

public sealed class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, ClientResponse<PagedResponse<AuditLogViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAuditLogQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<AuditLogViewModel>>> Handle(
        GetAuditLogQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        var page = await UnitOfWork.EventRecords.GetAuditLog(workspaceId, request.Filter, cancellationToken);

        return page;
    }
}
