using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class AiCredentialRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, UserAiCredential, Guid>(context, connectionFactory), IAiCredentialRepository
{
    public Task<List<UserAiCredential>> GetForUser(string userId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(credential => credential.UserId == userId)
            .OrderBy(credential => credential.Provider)
            .ToListAsync(cancellationToken);
    }

    public Task<UserAiCredential?> GetForProvider(
        string userId,
        AiProvider provider,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(credential => credential.UserId == userId && credential.Provider == provider)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<UserAiCredential?> GetOwned(
        Guid credentialId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(credential => credential.Id == credentialId && credential.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
