using System.Text.Json;

using Netptune.Core.BaseEntities;
using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Entities;

public record ImportSession : WorkspaceEntity<int>
{
    public const int DefaultRetentionDays = 7;

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public ImportStage Stage { get; set; } = ImportStage.Uploaded;

    public ImportSourceKind SourceKind { get; set; }

    public ImportVendorProfile VendorProfile { get; set; } = ImportVendorProfile.None;

    public string OriginalName { get; set; } = null!;

    public string StorageKey { get; set; } = null!;

    public long SizeBytes { get; set; }

    public JsonDocument? SourceProfile { get; set; }

    public JsonDocument? Mapping { get; set; }

    public JsonDocument? PreviewResult { get; set; }

    public JsonDocument? Result { get; set; }

    public string TargetRecordType { get; set; } = null!;

    public string? TargetProjectKey { get; set; }

    public string? TargetBoardIdentifier { get; set; }

    public string CreatedBy { get; set; } = null!;

    public int ProgressPercent { get; set; }

    public string? ProgressMessage { get; set; }

    public string? Error { get; set; }

    public bool QuotaReleased { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? CommittedAt { get; set; }

    public ICollection<ImportSessionEntry> Entries { get; set; } = new HashSet<ImportSessionEntry>();
}

public record ImportSessionEntry : KeyedEntity<long>
{
    public int SessionId { get; set; }

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public ImportEntryOperation Operation { get; set; }

    public JsonDocument? PreviousValues { get; set; }

    public DateTime? EntityUpdatedAt { get; set; }

    public ImportSession Session { get; set; } = null!;
}
