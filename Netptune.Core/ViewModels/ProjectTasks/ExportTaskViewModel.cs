using System;

namespace Netptune.Core.ViewModels.ProjectTasks
{
    public class ExportTaskViewModel
    {
        public int Id { get; set; }

        public string AssigneeId { get; set; }

        public string OwnerId { get; set; }

        public int ProjectScopeId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string SystemId { get; set; }

        public string Status { get; set; }

        public bool IsFlagged { get; set; }

        public double SortOrder { get; set; }

        public int? ProjectId { get; set; }

        public int? WorkspaceId { get; set; }

        public string WorkspaceSlug { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string AssigneeUsername { get; set; }

        public string AssigneePictureUrl { get; set; }

        public string OwnerUsername { get; set; }

        public string OwnerPictureUrl { get; set; }

        public string ProjectName { get; set; }

        public string Group { get; set; }
    }
}
