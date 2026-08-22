using System.Text.Json;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record TaskView : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Slug { get; set; } = null!;

    public string? Icon { get; set; }

    public JsonDocument Definition { get; set; } = null!;

    public bool IsShared { get; set; }
}
