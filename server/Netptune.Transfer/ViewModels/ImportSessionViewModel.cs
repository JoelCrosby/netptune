using Netptune.Transfer.Enums;

namespace Netptune.Transfer.ViewModels;

public sealed record ImportSessionViewModel
{
    public Guid PublicId { get; init; }

    public ImportStage Stage { get; init; }

    public ImportSourceKind SourceKind { get; init; }

    public ImportVendorProfile VendorProfile { get; init; }

    public string OriginalName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public string TargetRecordType { get; init; } = null!;

    public string? TargetProjectKey { get; init; }

    public string? TargetBoardIdentifier { get; init; }

    public int ProgressPercent { get; init; }

    public string? ProgressMessage { get; init; }

    public string? Error { get; init; }

    public int Created { get; init; }

    public int Updated { get; init; }

    public int Skipped { get; init; }

    public int Failed { get; init; }

    public bool CanUndo { get; init; }

    public string? CreatedByUserId { get; init; }

    public string? CreatedByDisplayName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? CommittedAt { get; init; }

    public DateTime ExpiresAt { get; init; }
}
