using Mediator;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetSprintBurndownReportQuery(int SprintId, ReportingUnit Unit, string TimeZone)
    : IRequest<ClientResponse<SprintBurndownReport>>;

public sealed class GetSprintBurndownReportQueryHandler : IRequestHandler<GetSprintBurndownReportQuery, ClientResponse<SprintBurndownReport>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetSprintBurndownReportQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<SprintBurndownReport>> Handle(GetSprintBurndownReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var workspaceKey = Identity.GetWorkspaceKey();
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

            if (!workspaceId.HasValue)
            {
                return ClientResponse<SprintBurndownReport>.NotFound;
            }

            var projectIds = await UnitOfWork.Projects.GetAllIdsInWorkspace(
                workspaceId.Value,
                cancellationToken: cancellationToken);

            var scope = new ReportingScope(workspaceId.Value, projectIds.ToHashSet());

            var report = await UnitOfWork.Reports.GetBurndown(
                scope,
                request.SprintId,
                request.Unit,
                request.TimeZone,
                cancellationToken);

            return report is null
                ? ClientResponse<SprintBurndownReport>.NotFound
                : ClientResponse<SprintBurndownReport>.Success(report);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<SprintBurndownReport>.Failed(exception.Message);
        }
    }
}
