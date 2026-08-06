using Netptune.Transfer.Enums;
using Netptune.Transfer.Definitions;
using Netptune.Transfer.Records;

namespace Netptune.Transfer.Services;

public sealed record ExportWriteRequest
{
    public required string RecordTypeName { get; init; }

    public required IReadOnlyList<TransferField> Fields { get; init; }

    public required IAsyncEnumerable<ExportRecord> Records { get; init; }

    public required ExportOptionsModel Options { get; init; }
}

public interface IExportWriter
{
    ExportFormat Format { get; }

    string ContentType { get; }

    string FileExtension { get; }

    Task<long> Write(ExportWriteRequest request, Stream output, CancellationToken cancellationToken = default);
}
