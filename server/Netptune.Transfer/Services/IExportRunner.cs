using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Transfer.Export;

namespace Netptune.Transfer.Services;

public sealed record ExportRunRequest
{
    public required int WorkspaceId { get; init; }

    public required string WorkspaceSlug { get; init; }

    public required ExportDefinitionModel Definition { get; init; }

    public int? MaxRecords { get; init; }

    public int InlineRowLimit { get; init; } = 10_000;
}

public sealed record ExportRunProgress
{
    public required int Percent { get; init; }

    public required string Message { get; init; }
}

public sealed record ExportRunResult
{
    public required Stream Content { get; init; }

    public required string ContentType { get; init; }

    public required string FileName { get; init; }

    public required long RowCount { get; init; }
}

public sealed record ExportPreviewResult
{
    public IReadOnlyList<string> FieldKeys { get; init; } = [];

    public IReadOnlyList<string> Headers { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    public long EstimatedRowCount { get; init; }

    public bool CanRunInline { get; init; }

    public long ArchiveFileBytes { get; init; }
}

// One preview row, keyed by field so the client addresses cells by field key rather than by position.
public sealed record ExportPreviewRow
{
    public required string Ref { get; init; }

    public required IReadOnlyDictionary<string, string> Values { get; init; }
}

public delegate Task ExportProgressReporter(ExportRunProgress progress, CancellationToken cancellationToken);

public interface IExportRunner
{
    Task<ExportRunResult> Run(ExportRunRequest request, ExportProgressReporter reportProgress, CancellationToken cancellationToken = default);

    Task<ExportPreviewResult> Preview(ExportRunRequest request, int sampleSize, CancellationToken cancellationToken = default);

    Task<PagedResponse<ExportPreviewRow>> PreviewRows(ExportRunRequest request, Pagination pagination, CancellationToken cancellationToken = default);
}
