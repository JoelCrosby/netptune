using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class RefreshTokenRepository : Repository<DataContext, RefreshToken, int>, IRefreshTokenRepository
{
    public RefreshTokenRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return Context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    // A single conditional UPDATE, so of two requests racing with the same token only one revokes it.
    public async Task<bool> TryRevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var revokedCount = await Context.RefreshTokens
            .Where(t => t.Token == token && t.Revoked == null && t.Expires > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Revoked, now), cancellationToken);

        return revokedCount == 1;
    }

    public Task<int> RemoveExpiredAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return Context.RefreshTokens
            .Where(t => t.UserId == userId && (t.Revoked != null || t.Expires <= now))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
