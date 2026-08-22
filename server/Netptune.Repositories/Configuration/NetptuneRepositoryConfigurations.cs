using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Repositories;
using Netptune.Repositories.Common;
using Netptune.Repositories.ConnectionFactories;
using Netptune.Repositories.UnitOfWork;

namespace Netptune.Repositories.Configuration;

public static class NetptuneRepositoryConfigurations
{
    public static IServiceCollection AddNetptuneRepository(this IServiceCollection services, Action<NetptuneRepositoryOptions> optionsAction)
    {

        if (optionsAction is null)
        {
            throw new ArgumentNullException(nameof(optionsAction));
        }

        var netptuneRepositoryOptions = new NetptuneRepositoryOptions();

        optionsAction(netptuneRepositoryOptions);

        services.Configure(optionsAction);

        services.AddScoped<IDbConnectionFactory>(_ => new NetptuneConnectionFactory(netptuneRepositoryOptions.ConnectionString));
        services.AddScoped<INetptuneUnitOfWork, NetptuneUnitOfWork>();
        services.AddScoped<IAdvisoryLock, PostgresAdvisoryLock>();

        services.AddScoped<IExportJobRepository, ExportJobRepository>();
        services.AddScoped<IExportDefinitionRepository, ExportDefinitionRepository>();
        services.AddScoped<ITaskViewRepository, TaskViewRepository>();

        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IArchiveRepository, ArchiveRepository>();
        services.AddScoped<IImportSessionRepository, ImportSessionRepository>();

        DapperTypeHandlers.Register();

        return services;
    }
}
