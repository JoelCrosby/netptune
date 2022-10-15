using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.ProjectTasks;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.Entities;

public class ProjectTask : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string Description { get; set; }  = null!;

    public ProjectTaskStatus Status { get; set; }

    public int ProjectScopeId { get; set; }

    public bool IsFlagged { get; set; }

    #region ForeignKeys

    public int? ProjectId { get; set; }

    #endregion

    #region NavigationProperties

    [JsonIgnore]
    public ICollection<ProjectTaskAppUser> ProjectTaskAppUsers { get; set; } = new HashSet<ProjectTaskAppUser>();

    [JsonIgnore]
    public Project? Project { get; set; }

    [JsonIgnore]
    public ICollection<ProjectTaskInBoardGroup> ProjectTaskInBoardGroups { get; set; } = new HashSet<ProjectTaskInBoardGroup>();

    [JsonIgnore]
    public ICollection<ProjectTaskTag> ProjectTaskTags { get; set; } = new HashSet<ProjectTaskTag>();

    [JsonIgnore]
    public ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();

    #endregion

    #region Methods

    public TaskViewModel ToViewModel()
    {
        // TODO: Implement Assignee View models

        return new()
        {
            Id = Id,
            OwnerId = OwnerId,
            Name = Name,
            Description = Description,
            Status = Status,
            ProjectScopeId = ProjectScopeId,
            SystemId = Project is null ? $"{ProjectScopeId}" : $"{Project.Key}-{ProjectScopeId}",
            IsFlagged = IsFlagged,
            ProjectId = ProjectId,
            WorkspaceId = WorkspaceId,
            WorkspaceKey = Workspace.Slug,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            OwnerUsername = Owner?.DisplayName ?? string.Empty,
            OwnerPictureUrl = Owner?.PictureUrl ?? string.Empty,
            ProjectName = Project?.Name ?? string.Empty,
            Tags = Tags.Select(x => x.Name).OrderBy(x => x).ToList(),
            Assignees = ProjectTaskAppUsers
                .Select(u => u.User)
                .Select(u => new AssigneeViewModel
                {
                    Id = u.Id,
                    DisplayName = $"{u.Firstname} {u.Lastname}",
                    PictureUrl = u.PictureUrl,
                })
                .ToList(),
        };
    }

    public ExportTaskViewModel ToExportViewModel()
    {
        // TODO: Implement Assignee View models

        return new()
        {
            Name = Name,
            Description = Description,
            Status = Status.ToString(),
            SystemId = Project is null ? $"{ProjectScopeId}" : $"{Project.Key}-{ProjectScopeId}",
            IsFlagged = IsFlagged,
            Board = Workspace.Slug,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Owner = Owner?.Email ?? string.Empty,
            Project = Project?.Name ?? string.Empty,
            Group = ProjectTaskInBoardGroups.FirstOrDefault()?.BoardGroup?.Name,
            Assignees = ProjectTaskAppUsers
                .Select(u => u.User)
                .Select(u => u.Email)
                .ToList(),
        };
    }

    #endregion
}
