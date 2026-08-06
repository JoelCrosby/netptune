using Netptune.Transfer.Entities;
using Netptune.Transfer.Import;

namespace Netptune.Transfer.Services;

public sealed record ImportApplyRequest
{
    public required int WorkspaceId { get; init; }

    public required string WorkspaceSlug { get; init; }

    public required string UserId { get; init; }

    public required ImportSession Session { get; init; }

    public required ImportMappingModel Mapping { get; init; }

    public required Stream Source { get; init; }

    public required IReadOnlyList<string> ColumnNames { get; init; }

    public required ImportReadOptions ReadOptions { get; init; }

    public bool SkipFailingRows { get; init; }

    // Row count the inspect stage saw, used to turn processed rows into a percentage.
    public long? EstimatedRowCount { get; init; }

    public int MaxRows { get; init; } = 250_000;

    public int PreviewRowCap { get; init; } = 5_000;
}

public sealed record ImportCommitResult
{
    public int Created { get; init; }

    public int Updated { get; init; }

    public int Skipped { get; init; }

    public int Failed { get; init; }
}

public sealed record ImportProgress
{
    public required int Percent { get; init; }

    public required string Message { get; init; }
}

public delegate Task ImportProgressReporter(ImportProgress progress, CancellationToken cancellationToken);

public interface IImportApplier
{
    Task<ImportPreviewResult> Preview(ImportApplyRequest request, CancellationToken cancellationToken = default);

    Task<ImportCommitResult> Commit(ImportApplyRequest request, ImportProgressReporter reportProgress, CancellationToken cancellationToken = default);
}
