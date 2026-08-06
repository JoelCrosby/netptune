using Microsoft.Extensions.DependencyInjection;

using Netptune.Transfer.Services;
using Netptune.Transfer.Undo;
using Netptune.Import.Archive;
using Netptune.Import.Undo;
using Netptune.Import.Vendors;

namespace Netptune.Import;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetptuneImport(this IServiceCollection services)
    {
        services.AddTransient<IImportSourceReader, CsvImportSourceReader>();
        services.AddTransient<IImportSourceReader, XlsxImportSourceReader>();
        services.AddTransient<IImportSourceReader>(_ => new JsonImportSourceReader(false));
        services.AddTransient<IImportSourceReader>(_ => new JsonImportSourceReader(true));

        services.AddTransient<IImportVendorProfile, JiraImportVendorProfile>();
        services.AddTransient<IImportVendorProfile, AsanaImportVendorProfile>();
        services.AddTransient<IImportVendorProfile, TrelloImportVendorProfile>();
        services.AddTransient<IImportVendorProfile, NetptuneImportVendorProfile>();
        services.AddSingleton<ImportMappingSuggester>();
        services.AddTransient<IImportMappingAdvisor, ImportMappingAdvisor>();

        services.AddTransient<IImportSourceStore, ImportSourceStore>();
        services.AddTransient<IImportApplier, ImportApplier>();
        services.AddTransient<IArchiveImporter, ArchiveImporter>();

        services.AddTransient<IEntityUndoHandler, TaskEntityUndoHandler>();
        services.AddTransient<EntityUndoCatalog>();
        services.AddTransient<IImportUndoService, ImportUndoService>();

        return services;
    }
}
