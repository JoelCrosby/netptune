using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.ProjectTasks;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Netptune.Core.Entities
{
    public class ProjectTask : WorkspaceEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public ProjectTaskStatus Status { get; set; }

        public int ProjectScopeId { get; set; }

        public bool IsFlagged { get; set; }

        #region ForeignKeys

        public string AssigneeId { get; set; }

        public int? ProjectId { get; set; }

        #endregion

        #region NavigationProperties

        [JsonIgnore]
        public AppUser Assignee { get; set; }

        [JsonIgnore]
        public Project Project { get; set; }

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
            return new()
            {
                Id = Id,
                AssigneeId = Assignee?.Id ?? string.Empty,
                OwnerId = OwnerId,
                Name = Name,
                Description = Description,
                Status = Status,
                ProjectScopeId = ProjectScopeId,
                SystemId = Project is null ? null : $"{Project.Key}-{ProjectScopeId}",
                IsFlagged = IsFlagged,
                ProjectId = ProjectId,
                WorkspaceId = WorkspaceId,
                WorkspaceKey = Workspace?.Slug,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                AssigneeUsername = Assignee?.DisplayName ?? string.Empty,
                AssigneePictureUrl = Assignee?.PictureUrl ?? string.Empty,
                OwnerUsername = Owner?.DisplayName ?? string.Empty,
                OwnerPictureUrl = Owner?.PictureUrl ?? string.Empty,
                ProjectName = Project?.Name ?? string.Empty,
                Tags = Tags.Select(x => x.Name).OrderBy(x => x).ToList(),
            };
        }

        public ExportTaskViewModel ToExportViewModel()
        {
            return new()
            {
                Name = Name,
                Description = Description,
                Status = Status.ToString(),
                SystemId = Project is null ? null : $"{Project.Key}-{ProjectScopeId}",
                IsFlagged = IsFlagged,
                Board = Workspace.Slug,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Assignee = Assignee?.Email ?? string.Empty,
                Owner = Owner?.Email ?? string.Empty,
                Project = Project?.Name ?? string.Empty,
                Group = ProjectTaskInBoardGroups.FirstOrDefault()?.BoardGroup?.Name,
            };
        }

        #endregion
    }
}
