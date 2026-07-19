using Mediator;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetFlowReportQuery(ReportingFilter Filter) : IRequest<ClientResponse<FlowReport>>;

public sealed class GetFlowReportQueryHandler : IRequestHandler<GetFlowReportQuery, ClientResponse<FlowReport>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetFlowReportQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<FlowReport>> Handle(GetFlowReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<FlowReport>.NotFound;
            }

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
