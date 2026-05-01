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

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var refreshToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (refreshToken is null) return;

        refreshToken.Revoked = DateTime.UtcNow;
    }

    public async Task RemoveExpiredAsync(string userId, CancellationToken cancellationToken = default)
    {
        var expired = await Context.RefreshTokens
            .Where(t => t.UserId == userId && (t.Revoked != null || t.Expires <= DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        Context.RefreshTokens.RemoveRange(expired);
    }
}
