using System.Data;

using Dapper;

using Netptune.Core.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories.Common;

public sealed class PostgresAdvisoryLock(IDbConnectionFactory connectionFactory) : IAdvisoryLock
{
    public async Task<IAsyncDisposable?> TryAcquire(long key, CancellationToken cancellationToken = default)
    {
        var connection = connectionFactory.StartConnection();

        try
        {
            var command = new CommandDefinition(SqlScripts.TryAcquireAdvisoryLock, new { key }, cancellationToken: cancellationToken);
            var acquired = await connection.ExecuteScalarAsync<bool>(command);

            if (!acquired)
            {
                connection.Dispose();

                return null;
            }

            return new AdvisoryLockHandle(connection, key);
        }
        catch
        {
            connection.Dispose();

            throw;
        }
    }

    private sealed class AdvisoryLockHandle(IDbConnection connection, long key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await connection.ExecuteAsync(new CommandDefinition(SqlScripts.ReleaseAdvisoryLock, new { key }));
            }
            finally
            {
                connection.Dispose();
            }
        }
    }
}
