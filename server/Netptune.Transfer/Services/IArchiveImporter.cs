using Netptune.Transfer.Archive;

namespace Netptune.Transfer.Services;

public enum ArchiveImportMode
{
    // Restore into an existing workspace that holds no projects yet.
    Restore = 0,

    // Create a new workspace from the archive's own workspace record.
    Clone = 1,
}

public sealed record ArchiveImportRequest
{
    public required Stream Archive { get; init; }

    public required string UserId { get; init; }

    public ArchiveImportMode Mode { get; init; }

    // Target workspace for ArchiveImportMode.Restore.
    public int? WorkspaceId { get; init; }

    // Slug for the workspace created by ArchiveImportMode.Clone.
    public string? TargetSlug { get; init; }

    public bool InviteUnmatchedMembers { get; init; }
}

public sealed record ArchiveImportPreview
{
    public required ArchiveManifest Manifest { get; init; }

    public IReadOnlyDictionary<string, long> CountsByType { get; init; } = new Dictionary<string, long>();

    public IReadOnlyList<string> UnmatchedMemberEmails { get; init; } = [];

    public long FileBytes { get; init; }

    public long RemainingQuotaBytes { get; init; }

    public IReadOnlyList<string> SchemaUpgrades { get; init; } = [];

    public IReadOnlyList<string> Blockers { get; init; } = [];
}

public sealed record ArchiveImportResult
{
    public int WorkspaceId { get; init; }

    public required string WorkspaceSlug { get; init; }

    public IReadOnlyDictionary<string, int> CreatedByType { get; init; } = new Dictionary<string, int>();

    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public interface IArchiveImporter
{
    Task<ArchiveImportPreview> Preview(ArchiveImportRequest request, CancellationToken cancellationToken = default);

    Task<ArchiveImportResult> Import(ArchiveImportRequest request, CancellationToken cancellationToken = default);
}
