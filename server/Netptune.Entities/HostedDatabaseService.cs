using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Entities.Contexts;

namespace Netptune.Entities;

public class HostedDatabaseService : IHostedService
{
    private readonly IServiceScopeFactory ScopeFactory;

    public HostedDatabaseService(IServiceScopeFactory scopeFactory)
    {
        ScopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await DatabaseSchema.EnsureCreatedAsync(context, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
