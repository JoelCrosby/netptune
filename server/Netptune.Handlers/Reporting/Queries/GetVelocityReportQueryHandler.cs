using Mediator;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetVelocityReportQuery(VelocityFilter Filter)
    : IRequest<ClientResponse<VelocityReport>>;

public sealed class GetVelocityReportQueryHandler : IRequestHandler<GetVelocityReportQuery, ClientResponse<VelocityReport>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetVelocityReportQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<VelocityReport>> Handle(GetVelocityReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<VelocityReport>.NotFound;
            }

            if (!scope.CanAccessProject(request.Filter.ProjectId))
            {
                return ClientResponse<VelocityReport>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetVelocity(
                scope,
                request.Filter,
                cancellationToken);

            return ClientResponse<VelocityReport>.Success(report);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<VelocityReport>.Failed(exception.Message);
        }
    }
}
