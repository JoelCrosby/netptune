using Netptune.Core.Models.Search;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages;

public sealed record PageIndexScope(int PageId, int WorkspaceId, string WorkspaceKey);

public static class PageSearchIndex
{
    public static Task IndexAfterSave(
        INetptuneUnitOfWork unitOfWork,
        IEventPublisher publisher,
        PageIndexScope scope,
        bool renamed,
        CancellationToken cancellationToken)
    {
        if (renamed)
        {
            return IndexSubtree(unitOfWork, publisher, scope, cancellationToken);
        }

        return publisher.IndexPages([scope.PageId], scope.WorkspaceKey);
    }

    public static async Task IndexSubtree(
        INetptuneUnitOfWork unitOfWork,
        IEventPublisher publisher,
        PageIndexScope scope,
        CancellationToken cancellationToken)
    {
        var subtreeIds = await unitOfWork.Pages.GetSubtreeIds(
            scope.PageId,
            scope.WorkspaceId,
            false,
            cancellationToken);

        await publisher.IndexPages(subtreeIds, scope.WorkspaceKey);
    }
}
