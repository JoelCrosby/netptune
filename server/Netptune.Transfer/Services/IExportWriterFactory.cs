using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Services;

public interface IExportWriterFactory
{
    IExportWriter Resolve(ExportFormat format);
}
