using Mediator;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetFlowCycleTimeQuery(ReportingFilter Filter, PageRequest Page)
    : IRequest<ClientResponse<PagedResponse<CycleTimeBucket>>>;

// The weekly cycle-time buckets behind the flow chart, paged for the table in the
// same card. Buckets are folded from the event ledger rather than queried, so the
// page is taken in memory from the same report the chart draws.
public sealed class GetFlowCycleTimeQueryHandler
    : IRequestHandler<GetFlowCycleTimeQuery, ClientResponse<PagedResponse<CycleTimeBucket>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetFlowCycleTimeQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<PagedResponse<CycleTimeBucket>>> Handle(
        GetFlowCycleTimeQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<PagedResponse<CycleTimeBucket>>.NotFound;
            }

            if (request.Filter.ProjectId.HasValue && !scope.CanAccessProject(request.Filter.ProjectId.Value))
            {
                return ClientResponse<PagedResponse<CycleTimeBucket>>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetFlow(scope, request.Filter, cancellationToken);
            var pagination = request.Page.GetPagination();

            var items = Sort(report.CycleTimeBuckets, request.Page)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToList();

            var page = new PagedResponse<CycleTimeBucket>(
                items,
                pagination.Page,
                pagination.PageSize,
                report.CycleTimeBuckets.Count);

            return ClientResponse<PagedResponse<CycleTimeBucket>>.Success(page);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<PagedResponse<CycleTimeBucket>>.Failed(exception.Message);
        }
    }

    // Newest first by default, so the first page carries the most recent weeks.
    private static IEnumerable<CycleTimeBucket> Sort(IEnumerable<CycleTimeBucket> buckets, PageRequest request)
    {
        var isDescending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "median" => Order(buckets, bucket => bucket.MedianCycleTimeHours, isDescending),
            "p85" => Order(buckets, bucket => bucket.P85CycleTimeHours, isDescending),
            "samples" => Order(buckets, bucket => bucket.SampleSize, isDescending),
            _ => Order(buckets, bucket => bucket.WeekStarting, isDescending),
        };
    }

    private static IEnumerable<CycleTimeBucket> Order<TKey>(
        IEnumerable<CycleTimeBucket> buckets,
        Func<CycleTimeBucket, TKey> key,
        bool isDescending)
    {
        var ordered = isDescending
            ? buckets.OrderByDescending(key)
            : buckets.OrderBy(key);

        return ordered.ThenByDescending(bucket => bucket.WeekStarting);
    }
}
