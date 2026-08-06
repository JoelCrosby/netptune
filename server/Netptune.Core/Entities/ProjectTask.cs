using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;

namespace Netptune.Core.Entities;

public record ProjectTask : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int StatusId { get; set; }

    public int ProjectScopeId { get; set; }

    public TaskPriority? Priority { get; set; }

    public EstimateType? EstimateType { get; set; }

    public decimal? EstimateValue { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? ExternalId { get; set; }

    #region ForeignKeys

    public int? ProjectId { get; set; }

    public int? SprintId { get; set; }

    #endregion

    #region NavigationProperties

    [JsonIgnore]
    public Status? Status { get; set; }

    [JsonIgnore]
    public ICollection<ProjectTaskAppUser> ProjectTaskAppUsers { get; set; } = new HashSet<ProjectTaskAppUser>();

    [JsonIgnore]
    public Project? Project { get; set; }

    [JsonIgnore]
    public Sprint? Sprint { get; set; }

    [JsonIgnore]
    public ICollection<ProjectTaskInBoardGroup> ProjectTaskInBoardGroups { get; set; } = new HashSet<ProjectTaskInBoardGroup>();

    [JsonIgnore]
    public ICollection<ProjectTaskTag> ProjectTaskTags { get; set; } = new HashSet<ProjectTaskTag>();

    [JsonIgnore]
    public ICollection<TaskFile> Files { get; set; } = new HashSet<TaskFile>();

    [JsonIgnore]
    public ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();

    #endregion
}
