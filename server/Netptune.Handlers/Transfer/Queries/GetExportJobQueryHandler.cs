using Netptune.Transfer.Repositories;
using Mediator;

using Netptune.Core.Services;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record GetExportJobQuery(Guid PublicId) : IRequest<ExportJobViewModel?>;

public sealed class GetExportJobQueryHandler : IRequestHandler<GetExportJobQuery, ExportJobViewModel?>
{
    private readonly IExportJobRepository ExportJobs;
    private readonly IIdentityService Identity;

    public GetExportJobQueryHandler(IIdentityService identity, IExportJobRepository exportJobs)
    {
        Identity = identity;
        ExportJobs = exportJobs;
    }

    public async ValueTask<ExportJobViewModel?> Handle(GetExportJobQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        return await ExportJobs.GetViewModel(request.PublicId, workspaceId, cancellationToken);
    }
}
