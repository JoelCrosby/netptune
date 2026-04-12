using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken, int>
{
    Task<RefreshToken?> GetByTokenAsync(string token);

    Task RevokeAsync(string token);

    Task RemoveExpiredAsync(string userId);
}
