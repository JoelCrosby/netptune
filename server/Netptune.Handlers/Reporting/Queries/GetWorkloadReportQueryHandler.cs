using Mediator;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetWorkloadReportQuery(ReportingFilter Filter) : IRequest<ClientResponse<WorkloadReport>>;

public sealed class GetWorkloadReportQueryHandler : IRequestHandler<GetWorkloadReportQuery, ClientResponse<WorkloadReport>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetWorkloadReportQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<WorkloadReport>> Handle(GetWorkloadReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<WorkloadReport>.NotFound;
            }

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
