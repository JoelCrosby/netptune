using Mediator;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetWorkloadReportQuery(ReportingFilter Filter) : IRequest<ClientResponse<WorkloadReport>>;

public sealed class GetWorkloadReportQueryHandler : IRequestHandler<GetWorkloadReportQuery, ClientResponse<WorkloadReport>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkloadReportQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<WorkloadReport>> Handle(GetWorkloadReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var workspaceKey = Identity.GetWorkspaceKey();
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

            if (!workspaceId.HasValue)
            {
                return ClientResponse<WorkloadReport>.NotFound;
            }

            var projectIds = await UnitOfWork.Projects.GetAllIdsInWorkspace(
                workspaceId.Value,
                cancellationToken: cancellationToken);

            var scope = new ReportingScope(workspaceId.Value, projectIds.ToHashSet());

            if (request.Filter.ProjectId.HasValue && !scope.CanAccessProject(request.Filter.ProjectId.Value))
            {
                return ClientResponse<WorkloadReport>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetWorkload(scope, request.Filter, cancellationToken);

            return ClientResponse<WorkloadReport>.Success(report);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<WorkloadReport>.Failed(exception.Message);
        }
    }
}
