using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

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

        // EnsureCreated does not evolve an existing database. Keep this additive,
        // idempotent upgrade here until the project adopts EF migrations.
        await context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE project_tasks ADD COLUMN IF NOT EXISTS due_date date",
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
