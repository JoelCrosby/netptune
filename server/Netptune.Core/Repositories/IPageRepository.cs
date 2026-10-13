using Netptune.Core.Entities;
using Netptune.Core.Models.Pages;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Core.Repositories;

public interface IPageRepository : IWorkspaceEntityRepository<Page, int>
{
    Task<Page?> GetLiveInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<Page?> GetLiveWithTags(int id, int workspaceId, CancellationToken cancellationToken = default);

    Task<Page?> GetArchivedInWorkspace(int id, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<PageTreeNodeViewModel>> GetTree(int workspaceId, string userId, CancellationToken cancellationToken = default);

    Task<PageViewModel?> GetViewModel(int id, int workspaceId, string userId, CancellationToken cancellationToken = default);

    Task<PageViewModel?> GetViewModelBySlug(string slug, int workspaceId, string userId, CancellationToken cancellationToken = default);

    Task<List<PageBodyViewModel>> GetBodies(int workspaceId, IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    Task<PagedResponse<PageBodyViewModel>> FindByText(int workspaceId, PageTextSearch search, CancellationToken cancellationToken = default);

    Task<List<ArchivedPageViewModel>> GetArchived(int workspaceId, CancellationToken cancellationToken = default);

    Task<double> GetNextSortOrder(int workspaceId, int? parentId, CancellationToken cancellationToken = default);

    Task<bool> TrySaveContent(PageContentUpdate update, CancellationToken cancellationToken = default);

    Task<List<int>> DeleteArchived(IReadOnlyCollection<int> ids, int workspaceId, CancellationToken cancellationToken = default);

    Task<bool> AnyLiveInWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetSlugs(int workspaceId, CancellationToken cancellationToken = default);

    Task<bool> MarkReviewed(int id, int workspaceId, string userId, CancellationToken cancellationToken = default);

    Task<List<int>> GetSubtreeIds(int pageId, int workspaceId, bool includeArchived, CancellationToken cancellationToken = default);

    Task<List<int>> GetArchivedTogetherIds(Page root, CancellationToken cancellationToken = default);

    Task<bool> AnyCreatedByOthers(IReadOnlyCollection<int> ids, string userId, CancellationToken cancellationToken = default);

    Task<List<int>> GetAncestorIds(int pageId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<PageSearchSource>> GetSearchSources(IReadOnlyCollection<int> ids, string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<PageSearchSource>> GetAllSearchSources(string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<PageOutlineEntry>> GetOutline(string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<PageOutlineEntry>> GetAncestorOutline(IReadOnlyCollection<int> pageIds, string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<RecentPageViewModel>> GetRecentlyVisited(int workspaceId, string userId, int take, CancellationToken cancellationToken = default);

    Task<List<PageNeedingReviewViewModel>> GetNeedingReview(PageReviewWindow window, CancellationToken cancellationToken = default);

    Task<List<PageChildViewModel>> GetChildren(int parentId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<int>> GetIdsTaggedWith(IReadOnlyCollection<int> tagIds, int workspaceId, CancellationToken cancellationToken = default);
}
