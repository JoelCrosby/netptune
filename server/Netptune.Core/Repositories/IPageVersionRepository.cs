using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Core.Repositories;

public interface IPageVersionRepository : IRepository<PageVersion, int>
{
    Task<PageVersion?> GetLatest(int pageId, CancellationToken cancellationToken = default);

    Task<List<PageVersionSummaryViewModel>> GetSummaries(int pageId, int workspaceId, CancellationToken cancellationToken = default);

    Task<PageVersionViewModel?> GetViewModel(int versionId, int pageId, int workspaceId, CancellationToken cancellationToken = default);
}
