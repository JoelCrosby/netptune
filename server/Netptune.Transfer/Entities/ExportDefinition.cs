using System.Text.Json;

using Netptune.Core.BaseEntities;
using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Entities;

public record ExportDefinition : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string RecordType { get; set; } = null!;

    public ExportFormat Format { get; set; }

    public JsonDocument Definition { get; set; } = null!;

    public bool IsShared { get; set; }
}
