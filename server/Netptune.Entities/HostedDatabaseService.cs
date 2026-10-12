using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Entities.Contexts;

namespace Netptune.Entities;

// The schema is created in StartingAsync, which the host runs before any hosted service's StartAsync,
// because services registered earlier (data protection loading its key ring) read tables at startup.
public class HostedDatabaseService : IHostedLifecycleService
{
    private readonly IServiceScopeFactory ScopeFactory;

    public HostedDatabaseService(IServiceScopeFactory scopeFactory)
    {
        ScopeFactory = scopeFactory;
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await DatabaseSchema.EnsureCreatedAsync(context, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
