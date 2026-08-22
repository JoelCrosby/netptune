using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record GetExportJobsQuery(PageRequest Page) : IRequest<ClientResponse<PagedResponse<ExportJobViewModel>>>;

public sealed class GetExportJobsQueryHandler : IRequestHandler<GetExportJobsQuery, ClientResponse<PagedResponse<ExportJobViewModel>>>
{
    private readonly IExportJobRepository ExportJobs;
    private readonly IIdentityService Identity;

    public GetExportJobsQueryHandler(IIdentityService identity, IExportJobRepository exportJobs)
    {
        Identity = identity;
        ExportJobs = exportJobs;
    }

    public async ValueTask<ClientResponse<PagedResponse<ExportJobViewModel>>> Handle(GetExportJobsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var jobs = await ExportJobs.GetExportJobs(workspaceId, request.Page, cancellationToken);

        return ClientResponse<PagedResponse<ExportJobViewModel>>.Success(jobs);
    }
}
