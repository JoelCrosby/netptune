using System.Text.Json.Serialization;
using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.Utilities;

namespace Netptune.Core.Entities;

public record WorkspaceFile : WorkspaceEntity<int>
{
    public string ContentId { get; set; } = UniqueIdBuilder.Generate();

    public WorkspaceFilePurpose Purpose { get; set; }

    public WorkspaceFileStatus Status { get; set; } = WorkspaceFileStatus.Pending;

    public string OriginalName { get; set; } = null!;

    public string StorageKey { get; set; } = null!;

    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    public bool QuotaReleased { get; set; }

    [JsonIgnore]
    public ICollection<TaskFile> TaskFiles { get; set; } = new HashSet<TaskFile>();
}
