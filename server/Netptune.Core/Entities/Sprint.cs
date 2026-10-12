using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record Sprint : WorkspaceEntity<int>
{
    public const int IdentifierMaxLength = 128;

    public string Name { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public string? Goal { get; set; }

    public SprintStatus Status { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int ProjectId { get; set; }

    [JsonIgnore]
    public Project? Project { get; set; }

    [JsonIgnore]
    public ICollection<ProjectTask> ProjectTasks { get; set; } = new HashSet<ProjectTask>();
}
