using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Core.Repositories;

public interface IPageLinkRepository : IRepository<PageLink, int>
{
    Task ReplaceExtracted(int pageId, IReadOnlyCollection<PageLink> links, CancellationToken cancellationToken = default);

    Task<List<PageLinkedTaskViewModel>> GetLinkedTasks(int pageId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<PageLinkedPageViewModel>> GetLinkedPages(int pageId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<PageBacklinkViewModel>> GetBacklinks(PageLinkTargetType targetType, string targetId, int workspaceId, CancellationToken cancellationToken = default);
}
