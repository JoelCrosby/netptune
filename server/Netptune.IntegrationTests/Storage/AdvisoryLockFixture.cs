using Netptune.Core.Repositories.Common;
using Netptune.Repositories.ConnectionFactories;

using Testcontainers.PostgreSql;

using Xunit;

namespace Netptune.IntegrationTests.Storage;

public sealed class AdvisoryLockFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:18.3").Build();

    public IDbConnectionFactory ConnectionFactory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        ConnectionFactory = new NetptuneConnectionFactory(DbContainer.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        await DbContainer.DisposeAsync();
    }
}
