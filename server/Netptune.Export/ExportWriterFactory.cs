using Netptune.Export.Writers;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Services;

namespace Netptune.Export;

public sealed class ExportWriterFactory : IExportWriterFactory
{
    public IExportWriter Resolve(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Csv => new CsvExportWriter(ExportFormat.Csv),
            ExportFormat.Tsv => new CsvExportWriter(ExportFormat.Tsv),
            ExportFormat.Xlsx => new XlsxExportWriter(),
            ExportFormat.Json => new JsonExportWriter(ExportFormat.Json),
            ExportFormat.Ndjson => new JsonExportWriter(ExportFormat.Ndjson),
            _ => throw new NotSupportedException($"Export format '{format}' is not supported yet."),
        };
    }
}
