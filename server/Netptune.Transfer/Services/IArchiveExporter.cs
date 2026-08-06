using Netptune.Transfer.Definitions;

namespace Netptune.Transfer.Services;

public sealed record ArchiveExportRequest
{
    public required int WorkspaceId { get; init; }

    public required string WorkspaceSlug { get; init; }

    public required ExportOptionsModel Options { get; init; }
}

public interface IArchiveExporter
{
    Task<ExportRunResult> Write(ArchiveExportRequest request, ExportProgressReporter reportProgress, CancellationToken cancellationToken = default);

    Task<long> EstimateFileBytes(int workspaceId, CancellationToken cancellationToken = default);
}
