using Mediator;
using Netptune.Core.Models.Audit;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Services.Audit.Queries;

public sealed class GetActivitySummaryQueryHandler : IRequestHandler<GetActivitySummaryQuery, ClientResponse<List<AuditActivityPoint>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetActivitySummaryQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<AuditActivityPoint>>> Handle(GetActivitySummaryQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        filter.WorkspaceId = await Identity.GetWorkspaceId();

        var points = await UnitOfWork.ActivityLogs.GetActivitySummary(filter);

        return points;
    }
}
