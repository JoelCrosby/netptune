using Mediator;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetWorkloadRowsQuery(ReportingFilter Filter, PageRequest Page)
    : IRequest<ClientResponse<PagedResponse<WorkloadRow>>>;

// One row per assignee. The report aggregates open tasks in memory, so the page is
// taken from the same rows the summary above the table counts.
public sealed class GetWorkloadRowsQueryHandler
    : IRequestHandler<GetWorkloadRowsQuery, ClientResponse<PagedResponse<WorkloadRow>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetWorkloadRowsQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<PagedResponse<WorkloadRow>>> Handle(
        GetWorkloadRowsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<PagedResponse<WorkloadRow>>.NotFound;
            }

            if (request.Filter.ProjectId.HasValue && !scope.CanAccessProject(request.Filter.ProjectId.Value))
            {
                return ClientResponse<PagedResponse<WorkloadRow>>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetWorkload(scope, request.Filter, cancellationToken);
            var pagination = request.Page.GetPagination();

            var items = Sort(report.Rows, request.Page)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToList();

            var page = new PagedResponse<WorkloadRow>(
                items,
                pagination.Page,
                pagination.PageSize,
                report.Rows.Count);

            return ClientResponse<PagedResponse<WorkloadRow>>.Success(page);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<PagedResponse<WorkloadRow>>.Failed(exception.Message);
        }
    }

    // Heaviest load first by default, which is what the table is read for.
    private static IEnumerable<WorkloadRow> Sort(IEnumerable<WorkloadRow> rows, PageRequest request)
    {
        var isDescending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "displayname" => Order(rows, row => row.DisplayName, isDescending),
            "taskcount" => Order(rows, row => row.TaskCount, isDescending),
            _ => Order(rows, row => row.Value, isDescending),
        };
    }

    private static IEnumerable<WorkloadRow> Order<TKey>(
        IEnumerable<WorkloadRow> rows,
        Func<WorkloadRow, TKey> key,
        bool isDescending)
    {
        var ordered = isDescending ? rows.OrderByDescending(key) : rows.OrderBy(key);

        return ordered.ThenBy(row => row.DisplayName);
    }
}
