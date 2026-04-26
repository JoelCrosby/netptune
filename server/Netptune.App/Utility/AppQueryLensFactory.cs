using EFQueryLens.Core;

using Microsoft.EntityFrameworkCore;

using Netptune.Entities.Contexts;

namespace EFQueryLens.Core
{
    public interface IQueryLensDbContextFactory<out TContext>
        where TContext : DbContext
    {
        TContext CreateOfflineContext();
    }
}


namespace Netptune.App.Utility
{
    // ReSharper disable once UnusedType.Global
    public sealed class AppQueryLensFactory : IQueryLensDbContextFactory<DataContext>
    {
        public DataContext CreateOfflineContext()
        {
            const string connectionString = "Host=ef_querylens_offline;Database=ef_querylens_offline;Username=ef_querylens_offline;Password=ef_querylens_offline";
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new DataContext(options);
        }
    }
}
