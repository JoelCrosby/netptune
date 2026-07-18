using Mediator;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetVelocityReportQuery(int ProjectId, ReportingUnit Unit, int Take)
    : IRequest<ClientResponse<VelocityReport>>;

public sealed class GetVelocityReportQueryHandler : IRequestHandler<GetVelocityReportQuery, ClientResponse<VelocityReport>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetVelocityReportQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<VelocityReport>> Handle(GetVelocityReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var workspaceKey = Identity.GetWorkspaceKey();
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

            if (!workspaceId.HasValue)
            {
                return ClientResponse<VelocityReport>.NotFound;
            }

            var projectIds = await UnitOfWork.Projects.GetAllIdsInWorkspace(
                workspaceId.Value,
                cancellationToken: cancellationToken);

            var scope = new ReportingScope(workspaceId.Value, projectIds.ToHashSet());

            if (!scope.CanAccessProject(request.ProjectId))
            {
                return ClientResponse<VelocityReport>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetVelocity(
                scope,
                request.ProjectId,
                request.Unit,
                request.Take,
                cancellationToken);

            return ClientResponse<VelocityReport>.Success(report);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<VelocityReport>.Failed(exception.Message);
        }
    }
}
