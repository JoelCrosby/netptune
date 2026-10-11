namespace Netptune.Core.Repositories;

public interface IPageFavouriteRepository
{
    Task Add(int pageId, string userId, CancellationToken cancellationToken = default);

    Task<int> Remove(int pageId, string userId, CancellationToken cancellationToken = default);
}
