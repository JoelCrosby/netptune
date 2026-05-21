using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ICommandPaletteRecentItemRepository : IRepository<CommandPaletteRecentItem, int>
{
    Task<List<CommandPaletteRecentItemResponse>> GetRecentItems(
        string userId,
        int workspaceId,
        string scope,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CommandPaletteRecentItem?> GetByUrl(
        string userId,
        int workspaceId,
        string url,
        CancellationToken cancellationToken = default);

    Task<List<CommandPaletteRecentItem>> GetStaleWorkspaceItems(
        string userId,
        int workspaceId,
        int skip,
        CancellationToken cancellationToken = default);

    Task<List<CommandPaletteRecentItem>> GetClearableItems(
        string userId,
        int workspaceId,
        string scope,
        CancellationToken cancellationToken = default);
}
