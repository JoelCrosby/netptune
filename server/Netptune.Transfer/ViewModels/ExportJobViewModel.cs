using Netptune.Transfer.Enums;

namespace Netptune.Transfer.ViewModels;

public sealed record ExportJobViewModel
{
    public Guid PublicId { get; init; }

    public ExportJobStatus Status { get; init; }

    public string RecordType { get; init; } = null!;

    public ExportFormat Format { get; init; }

    public string? Name { get; init; }

    public string? FileName { get; init; }

    public long? RowCount { get; init; }

    public long? SizeBytes { get; init; }

    public int ProgressPercent { get; init; }

    public string? ProgressMessage { get; init; }

    public string? Error { get; init; }

    public bool HasArtefact { get; init; }

    public string? RequestedByUserId { get; init; }

    public string? RequestedByDisplayName { get; init; }

    public string? RequestedByPictureUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public DateTime ExpiresAt { get; init; }
}
