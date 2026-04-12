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

    public Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return Context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task RevokeAsync(string token)
    {
        var refreshToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (refreshToken is null) return;

        refreshToken.Revoked = DateTime.UtcNow;
    }

    public async Task RemoveExpiredAsync(string userId)
    {
        var expired = await Context.RefreshTokens
            .Where(t => t.UserId == userId && (t.Revoked != null || t.Expires <= DateTime.UtcNow))
            .ToListAsync();

        Context.RefreshTokens.RemoveRange(expired);
    }
}
