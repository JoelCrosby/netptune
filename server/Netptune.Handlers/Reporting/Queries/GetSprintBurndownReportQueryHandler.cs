using Mediator;

using Netptune.Core.Models.Reporting;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetSprintBurndownReportQuery(SprintBurndownFilter Filter)
    : IRequest<ClientResponse<SprintBurndownReport>>;

public sealed class GetSprintBurndownReportQueryHandler : IRequestHandler<GetSprintBurndownReportQuery, ClientResponse<SprintBurndownReport>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetSprintBurndownReportQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<SprintBurndownReport>> Handle(GetSprintBurndownReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<SprintBurndownReport>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetBurndown(
                scope,
                request.Filter,
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
