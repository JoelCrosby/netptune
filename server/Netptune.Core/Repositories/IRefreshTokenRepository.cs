using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken, int>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<bool> TryRevokeAsync(string token, CancellationToken cancellationToken = default);

    Task<int> RemoveExpiredAsync(string userId, CancellationToken cancellationToken = default);
}
