using System.Diagnostics;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Authorization;
using Netptune.Entities;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData;

public sealed class SeedDataRunner
{
    private readonly IServiceProvider ServiceProvider;
    private readonly ILogger<SeedDataRunner> Logger;

    public SeedDataRunner(IServiceProvider serviceProvider, ILogger<SeedDataRunner> logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        Logger.LogInformation("{Service} starting data seed execution", nameof(SeedDataRunner));

        var timer = Stopwatch.StartNew();

        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var seeders = scope.ServiceProvider.GetServices<ISeeder>();

        await DatabaseSchema.EnsureCreatedAsync(dbContext, ct);

        if (await dbContext.Users.AnyAsync(ct))
        {
            await SyncOwnerPermissions(dbContext, ct);
            Logger.LogInformation("{Service} data already present, skipping seed", nameof(SeedDataRunner));
            return;
        }

        var seedContext = new SeedContext();
        var phases = seeders.GroupBy(s => s.Phase).OrderBy(g => g.Key);

        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
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
        });

        timer.Stop();

        Logger.LogInformation("{Service} finished execution in {Elapsed}", nameof(SeedDataRunner), $"{timer.ElapsedMilliseconds:N}ms");
    }

    private static async Task SyncOwnerPermissions(DataContext dbContext, CancellationToken ct)
    {
        var owners = await dbContext.WorkspaceAppUsers
            .Where(workspaceUser => workspaceUser.Role == WorkspaceRole.Owner)
            .ToListAsync(ct);
        var ownerPermissions = WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Owner);

        foreach (var owner in owners)
        {
            owner.Permissions = ownerPermissions.ToList();
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
