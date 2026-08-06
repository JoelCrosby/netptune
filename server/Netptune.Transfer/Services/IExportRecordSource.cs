using Netptune.Transfer.Definitions;
using Netptune.Transfer.Records;

namespace Netptune.Transfer.Services;

public sealed record ExportRecordQuery
{
    public required int WorkspaceId { get; init; }

    public required string WorkspaceSlug { get; init; }

    public required ExportDefinitionModel Definition { get; init; }

    public int? MaxRecords { get; init; }
}

public interface IExportRecordSource
{
    bool CanRead(string recordType);

    IReadOnlyList<TransferField> ResolveFields(ExportDefinitionModel definition);

    IAsyncEnumerable<ExportRecord> Read(ExportRecordQuery query, CancellationToken cancellationToken = default);

    Task<long> EstimateCount(ExportRecordQuery query, CancellationToken cancellationToken = default);
}
