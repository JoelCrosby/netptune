using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IAiCredentialRepository : IRepository<UserAiCredential, Guid>
{
    Task<List<UserAiCredential>> GetForUser(string userId, CancellationToken cancellationToken = default);

    Task<UserAiCredential?> GetForProvider(string userId, AiProvider provider, CancellationToken cancellationToken = default);

    Task<UserAiCredential?> GetOwned(Guid credentialId, string userId, CancellationToken cancellationToken = default);
}
