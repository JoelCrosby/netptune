using System.Text.Json;

using Netptune.Core.BaseEntities;
using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Entities;

public record ExportJob : WorkspaceEntity<int>
{
    public const int DefaultRetentionDays = 7;

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public ExportJobStatus Status { get; set; } = ExportJobStatus.Pending;

    public string RecordType { get; set; } = null!;

    public ExportFormat Format { get; set; }

    public JsonDocument Definition { get; set; } = null!;

    public int? DefinitionId { get; set; }

    public string RequestedBy { get; set; } = null!;

    public string? Name { get; set; }

    public long? RowCount { get; set; }

    public long? SizeBytes { get; set; }

    public string? StorageKey { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public int ProgressPercent { get; set; }

    public string? ProgressMessage { get; set; }

    public string? Error { get; set; }

    public bool QuotaReleased { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
