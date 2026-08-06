using Microsoft.Extensions.DependencyInjection;

using Netptune.Transfer.Services;

namespace Netptune.Export;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneExport(this IServiceCollection services)
    {
        services.AddSingleton<IExportWriterFactory, ExportWriterFactory>();
        services.AddTransient<IExportRunner, ExportRunner>();
        services.AddTransient<IArchiveExporter, ArchiveExporter>();
        services.AddTransient<IExportRecordSource, TaskExportRecordSource>();

        return services;
    }
}
