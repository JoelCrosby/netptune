using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Repositories.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Repositories.Common;
using Netptune.Repositories.ConnectionFactories;

namespace Netptune.Repositories.Configuration
{
    public static class NetptuneRepositoryConfigurations
    {
        public static void AddNetptuneRepository(this IServiceCollection services, string connectionString)
        {
            services.AddScoped<IDbConnectionFactory>(_ => new NetptuneConnectionFactory(connectionString));
            services.AddScoped<INetptuneUnitOfWork, NetptuneUnitOfWork>();
        }
    }
}
