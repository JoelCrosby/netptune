using Netptune.Transfer.Services;

namespace Netptune.Transfer.ViewModels;

public sealed record ArchiveImportPreviewViewModel
{
    public required int SchemaVersion { get; init; }

    public required string WorkspaceName { get; init; }

    public required string WorkspaceSlug { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyDictionary<string, long> CountsByType { get; init; } = new Dictionary<string, long>();

    public IReadOnlyList<string> UnmatchedMemberEmails { get; init; } = [];

    public long FileBytes { get; init; }

    public long RemainingQuotaBytes { get; init; }

    public IReadOnlyList<string> SchemaUpgrades { get; init; } = [];

    public IReadOnlyList<string> Blockers { get; init; } = [];

    public static ArchiveImportPreviewViewModel From(ArchiveImportPreview preview)
    {
        return new ArchiveImportPreviewViewModel
        {
            SchemaVersion = preview.Manifest.SchemaVersion,
            WorkspaceName = preview.Manifest.Workspace.Name,
            WorkspaceSlug = preview.Manifest.Workspace.Slug,
            CreatedAt = preview.Manifest.CreatedAt,
            CountsByType = preview.CountsByType,
            UnmatchedMemberEmails = preview.UnmatchedMemberEmails,
            FileBytes = preview.FileBytes,
            RemainingQuotaBytes = preview.RemainingQuotaBytes,
            SchemaUpgrades = preview.SchemaUpgrades,
            Blockers = preview.Blockers,
        };
    }
}

public sealed record ArchiveImportResultViewModel
{
    public int WorkspaceId { get; init; }

    public required string WorkspaceSlug { get; init; }

    public IReadOnlyDictionary<string, int> CreatedByType { get; init; } = new Dictionary<string, int>();

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public static ArchiveImportResultViewModel From(ArchiveImportResult result)
    {
        return new ArchiveImportResultViewModel
        {
            WorkspaceId = result.WorkspaceId,
            WorkspaceSlug = result.WorkspaceSlug,
            CreatedByType = result.CreatedByType,
            Warnings = result.Warnings,
        };
    }
}
