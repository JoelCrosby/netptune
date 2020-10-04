using System;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Repositories.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Repositories.ConnectionFactories;
using Netptune.Repositories.UnitOfWork;

namespace Netptune.Repositories.Configuration
{
    public static class NetptuneRepositoryConfigurations
    {
        public static void AddNetptuneRepository(this IServiceCollection services, Action<NetptuneRepositoryOptions> optionsAction)
        {
            if (optionsAction is null)
                throw new ArgumentNullException(nameof(optionsAction));

            var netptuneRepositoryOptions = new NetptuneRepositoryOptions();

            optionsAction(netptuneRepositoryOptions);

            services.Configure(optionsAction);

            services.AddScoped<IDbConnectionFactory>(_ => new NetptuneConnectionFactory(netptuneRepositoryOptions.ConnectionString));
            services.AddScoped<INetptuneUnitOfWork, NetptuneUnitOfWork>();
        }
    }
}
