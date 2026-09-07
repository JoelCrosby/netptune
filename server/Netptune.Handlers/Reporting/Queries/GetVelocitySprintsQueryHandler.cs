using Mediator;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetVelocitySprintsQuery(VelocityFilter Filter, PageRequest Page)
    : IRequest<ClientResponse<PagedResponse<VelocityPoint>>>;

// One row per recent sprint, paged for the table inside the velocity card. The
// filter's Take still bounds how many sprints the report covers.
public sealed class GetVelocitySprintsQueryHandler
    : IRequestHandler<GetVelocitySprintsQuery, ClientResponse<PagedResponse<VelocityPoint>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetVelocitySprintsQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<PagedResponse<VelocityPoint>>> Handle(
        GetVelocitySprintsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<PagedResponse<VelocityPoint>>.NotFound;
            }

            if (!scope.CanAccessProject(request.Filter.ProjectId))
            {
                return ClientResponse<PagedResponse<VelocityPoint>>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetVelocity(scope, request.Filter, cancellationToken);
            var pagination = request.Page.GetPagination();

            var items = Sort(report.Sprints, request.Page)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToList();

            var page = new PagedResponse<VelocityPoint>(
                items,
                pagination.Page,
                pagination.PageSize,
                report.Sprints.Count);

            return ClientResponse<PagedResponse<VelocityPoint>>.Success(page);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<PagedResponse<VelocityPoint>>.Failed(exception.Message);
        }
    }

    // Most recently completed first by default.
    private static IEnumerable<VelocityPoint> Sort(IEnumerable<VelocityPoint> sprints, PageRequest request)
    {
        var isDescending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "sprintname" => Order(sprints, sprint => sprint.SprintName, isDescending),
            "committed" => Order(sprints, sprint => sprint.Committed, isDescending),
            "completed" => Order(sprints, sprint => sprint.Completed, isDescending),
            "missingestimatecount" => Order(sprints, sprint => sprint.MissingEstimateCount, isDescending),
            "differentunitestimatecount" => Order(sprints, sprint => sprint.DifferentUnitEstimateCount, isDescending),
            _ => Order(sprints, sprint => sprint.CompletedAt, isDescending),
        };
    }

    private static IEnumerable<VelocityPoint> Order<TKey>(
        IEnumerable<VelocityPoint> sprints,
        Func<VelocityPoint, TKey> key,
        bool isDescending)
    {
        var ordered = isDescending ? sprints.OrderByDescending(key) : sprints.OrderBy(key);

        return ordered.ThenByDescending(sprint => sprint.CompletedAt);
    }
}
