namespace Netptune.Core.Repositories;

public interface IPageVisitRepository
{
    Task Record(int pageId, string userId, CancellationToken cancellationToken = default);
}
