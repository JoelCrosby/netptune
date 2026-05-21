using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Preferences;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class CommandPaletteRecentItemRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, CommandPaletteRecentItem, int>(context, connectionFactory), ICommandPaletteRecentItemRepository
{
    public Task<List<CommandPaletteRecentItemResponse>> GetRecentItems(
        string userId,
        int workspaceId,
        string scope,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return BuildScopedQuery(userId, workspaceId, scope)
            .OrderByDescending(item => item.LastAccessedAt)
            .Take(limit)
            .Select(item => new CommandPaletteRecentItemResponse
            {
                Type = item.Type,
                EntityId = item.EntityId,
                Title = item.Title,
                Url = item.Url,
                LastAccessedAt = item.LastAccessedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<CommandPaletteRecentItem?> GetByUrl(
        string userId,
        int workspaceId,
        string url,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .FirstOrDefaultAsync(
                item =>
                    item.UserId == userId &&
                    item.WorkspaceId == workspaceId &&
                    item.Url == url,
                cancellationToken);
    }

    public Task<List<CommandPaletteRecentItem>> GetStaleWorkspaceItems(
        string userId,
        int workspaceId,
        int skip,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(item => item.UserId == userId && item.WorkspaceId == workspaceId)
            .OrderByDescending(item => item.LastAccessedAt)
            .Skip(skip)
            .ToListAsync(cancellationToken);
    }

    public Task<List<CommandPaletteRecentItem>> GetClearableItems(
        string userId,
        int workspaceId,
        string scope,
        CancellationToken cancellationToken = default)
    {
        return (scope == PreferenceScopes.Global
                ? Entities.Where(item => item.UserId == userId)
                : Entities.Where(item => item.UserId == userId && item.WorkspaceId == workspaceId))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<CommandPaletteRecentItem> BuildScopedQuery(
        string userId,
        int workspaceId,
        string scope)
    {
        var query = Entities
            .AsNoTracking()
            .Where(item => item.UserId == userId);

        if (scope == PreferenceScopes.Global)
        {
            return query.Where(item => Context.WorkspaceAppUsers
                .Any(workspaceUser =>
                    workspaceUser.UserId == userId &&
                    workspaceUser.WorkspaceId == item.WorkspaceId));
        }

        return query.Where(item => item.WorkspaceId == workspaceId);
    }
}
