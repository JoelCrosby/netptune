using Mediator;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetFlowReportQuery(ReportingFilter Filter) : IRequest<ClientResponse<FlowReport>>;

public sealed class GetFlowReportQueryHandler : IRequestHandler<GetFlowReportQuery, ClientResponse<FlowReport>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetFlowReportQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<FlowReport>> Handle(GetFlowReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var workspaceKey = Identity.GetWorkspaceKey();
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

            if (!workspaceId.HasValue)
            {
                return ClientResponse<FlowReport>.NotFound;
            }

            var projectIds = await UnitOfWork.Projects.GetAllIdsInWorkspace(
                workspaceId.Value,
                cancellationToken: cancellationToken);

            var scope = new ReportingScope(workspaceId.Value, projectIds.ToHashSet());

            if (request.Filter.ProjectId.HasValue && !scope.CanAccessProject(request.Filter.ProjectId.Value))
            {
                return ClientResponse<FlowReport>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetFlow(scope, request.Filter, cancellationToken);

            return ClientResponse<FlowReport>.Success(report);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<FlowReport>.Failed(exception.Message);
        }
    }
}
