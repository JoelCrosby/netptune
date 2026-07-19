using Netptune.Core.Models.Reporting;
using Netptune.Core.Services;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Reporting;

public sealed class ReportingScopeResolver : IReportingScopeResolver
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public ReportingScopeResolver(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async Task<ReportingScope?> Resolve(CancellationToken cancellationToken = default)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (!workspaceId.HasValue)
        {
            return null;
        }

        var projectIds = await UnitOfWork.Projects.GetAllIdsInWorkspace(
            workspaceId.Value,
            cancellationToken: cancellationToken);

        return new ReportingScope(workspaceId.Value, projectIds.ToHashSet());
    }
}
