using Dapper;

using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public sealed class PageVisitRepository(IDbConnectionFactory connectionFactory) : ReadOnlyRepository(connectionFactory), IPageVisitRepository
{
    public async Task Record(int pageId, string userId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.UpsertPageVisit,
            new { PageId = pageId, UserId = userId },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }
}
