using System.Diagnostics;

using Microsoft.EntityFrameworkCore;

using Netptune.Entities.Contexts;

namespace Netptune.SeedData;

public sealed class SeedDataService : IHostedService
{
    private readonly IServiceProvider ServiceProvider;
    private readonly ILogger<SeedDataService> Logger;

    public SeedDataService(IServiceProvider serviceProvider, ILogger<SeedDataService> logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        Logger.LogInformation("{Service} starting data seed execution", nameof(SeedDataService));

        var timer = Stopwatch.StartNew();

        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var seeders = scope.ServiceProvider.GetServices<ISeeder>();

        await dbContext.Database.EnsureCreatedAsync(ct);

        if (await dbContext.Users.AnyAsync(ct))
        {
            Logger.LogInformation("{Service} data already present, skipping seed", nameof(SeedDataService));
            return;
        }

        var seedContext = new SeedContext();
        var phases = seeders.GroupBy(s => s.Phase).OrderBy(g => g.Key);

        await dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var phase in phases)
            {
                foreach (var seeder in phase)
                    await seeder.SeedAsync(dbContext, seedContext, ct);

                await dbContext.SaveChangesAsync(ct);
            }

            await dbContext.Database.CommitTransactionAsync(ct);
        }
        catch
        {
            await dbContext.Database.RollbackTransactionAsync(ct);
            throw;
        }

        timer.Stop();

        Logger.LogInformation("{Service} finished execution in {Elapsed}", nameof(SeedDataService), $"{timer.ElapsedMilliseconds:N}ms");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
