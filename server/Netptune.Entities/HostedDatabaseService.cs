using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Netptune.Entities.Contexts;

using Polly;
using Polly.Contrib.WaitAndRetry;

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
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(2), retryCount: 5);

        await Policy
            .Handle<Exception>()
            .WaitAndRetry(delay)
            .Execute(() => context.Database.EnsureCreatedAsync(cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
