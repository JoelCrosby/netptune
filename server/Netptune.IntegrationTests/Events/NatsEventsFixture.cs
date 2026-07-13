using Testcontainers.Nats;

using Xunit;

namespace Netptune.IntegrationTests.Events;

// Deliberately separate from NetptuneFixture's broker — these tests create, mutate and delete the
// netptune-events stream, which the api's publisher also owns.
public sealed class NatsEventsFixture : IAsyncLifetime
{
    private readonly NatsContainer Container = new NatsBuilder("nats:alpine")
        .WithCommand("-js")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public ValueTask InitializeAsync()
    {
        return new (Container.StartAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync().ConfigureAwait(false);
    }
}
