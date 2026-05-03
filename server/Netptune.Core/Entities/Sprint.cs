using System.Text.Json.Serialization;
using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Core.Entities;

public record Sprint : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string? Goal { get; set; }

    public SprintStatus Status { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int ProjectId { get; set; }

    [JsonIgnore]
    public Project Project { get; set; } = null!;

    [JsonIgnore]
    public ICollection<ProjectTask> ProjectTasks { get; set; } = new HashSet<ProjectTask>();

    public SprintViewModel ToViewModel()
    {
        return new SprintViewModel
        {
            Id = Id,
            Name = Name,
            Goal = Goal,
            Status = Status,
            StartDate = StartDate,
            EndDate = EndDate,
            CompletedAt = CompletedAt,
            ProjectId = ProjectId,
            ProjectName = Project.Name,
            ProjectKey = Project.Key,
            WorkspaceId = WorkspaceId,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            TaskCount = ProjectTasks.Count(task => !task.IsDeleted),
        };
    }
}
