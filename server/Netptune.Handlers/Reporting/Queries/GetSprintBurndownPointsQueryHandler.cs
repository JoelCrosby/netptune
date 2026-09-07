using Mediator;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetSprintBurndownPointsQuery(SprintBurndownFilter Filter, PageRequest Page)
    : IRequest<ClientResponse<PagedResponse<BurndownPoint>>>;

// The daily burndown points behind the sprint chart, paged for the table in the
// same card. Points are derived in memory, so the page is taken from the same
// report the chart draws.
public sealed class GetSprintBurndownPointsQueryHandler
    : IRequestHandler<GetSprintBurndownPointsQuery, ClientResponse<PagedResponse<BurndownPoint>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetSprintBurndownPointsQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<PagedResponse<BurndownPoint>>> Handle(
        GetSprintBurndownPointsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<PagedResponse<BurndownPoint>>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetBurndown(scope, request.Filter, cancellationToken);

            if (report is null)
            {
                return ClientResponse<PagedResponse<BurndownPoint>>.NotFound;
            }

            var pagination = request.Page.GetPagination();

            var items = Sort(report.Points, request.Page)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToList();

            var page = new PagedResponse<BurndownPoint>(
                items,
                pagination.Page,
                pagination.PageSize,
                report.Points.Count);

            return ClientResponse<PagedResponse<BurndownPoint>>.Success(page);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<PagedResponse<BurndownPoint>>.Failed(exception.Message);
        }
    }

    // Newest first by default, so the first page carries where the sprint stands now.
    private static IEnumerable<BurndownPoint> Sort(IEnumerable<BurndownPoint> points, PageRequest request)
    {
        var isDescending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "remaining" => Order(points, point => point.Remaining, isDescending),
            "totalscope" => Order(points, point => point.TotalScope, isDescending),
            "ideal" => Order(points, point => point.Ideal, isDescending),
            _ => Order(points, point => point.Date, isDescending),
        };
    }

    private static IEnumerable<BurndownPoint> Order<TKey>(
        IEnumerable<BurndownPoint> points,
        Func<BurndownPoint, TKey> key,
        bool isDescending)
    {
        var ordered = isDescending ? points.OrderByDescending(key) : points.OrderBy(key);

        return ordered.ThenByDescending(point => point.Date);
    }
}
