using Microsoft.EntityFrameworkCore.Design;

namespace Netptune.Entities.Contexts;

public sealed class DesignTimeDataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        return new DataContext();
    }
}
