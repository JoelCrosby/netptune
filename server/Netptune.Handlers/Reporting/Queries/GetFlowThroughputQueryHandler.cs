using Mediator;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Reporting.Queries;

public sealed record GetFlowThroughputQuery(ReportingFilter Filter, PageRequest Page)
    : IRequest<ClientResponse<PagedResponse<FlowBucket>>>;

public sealed class GetFlowThroughputQueryHandler
    : IRequestHandler<GetFlowThroughputQuery, ClientResponse<PagedResponse<FlowBucket>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetFlowThroughputQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<PagedResponse<FlowBucket>>> Handle(
        GetFlowThroughputQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var scope = await ScopeResolver.Resolve(cancellationToken);

            if (scope is null)
            {
                return ClientResponse<PagedResponse<FlowBucket>>.NotFound;
            }

            if (request.Filter.ProjectId.HasValue && !scope.CanAccessProject(request.Filter.ProjectId.Value))
            {
                return ClientResponse<PagedResponse<FlowBucket>>.NotFound;
            }

            var report = await UnitOfWork.Reports.GetFlow(scope, request.Filter, cancellationToken);
            var pagination = request.Page.GetPagination();

            var items = Sort(report.Buckets, request.Page)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToList();

            var page = new PagedResponse<FlowBucket>(
                items,
                pagination.Page,
                pagination.PageSize,
                report.Buckets.Count);

            return ClientResponse<PagedResponse<FlowBucket>>.Success(page);
        }
        catch (InvalidReportingFilterException exception)
        {
            return ClientResponse<PagedResponse<FlowBucket>>.Failed(exception.Message);
        }
    }

    private static IEnumerable<FlowBucket> Sort(IEnumerable<FlowBucket> buckets, PageRequest request)
    {
        var isDescending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "completed" => isDescending
                ? buckets.OrderByDescending(bucket => bucket.Completed).ThenByDescending(bucket => bucket.Date)
                : buckets.OrderBy(bucket => bucket.Completed).ThenBy(bucket => bucket.Date),
            _ => isDescending
                ? buckets.OrderByDescending(bucket => bucket.Date)
                : buckets.OrderBy(bucket => bucket.Date),
        };
    }
}
