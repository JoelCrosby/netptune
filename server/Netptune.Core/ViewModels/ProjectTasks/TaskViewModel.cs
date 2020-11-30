using System;
using System.Collections.Generic;

using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.ProjectTasks
{
    public class TaskViewModel
    {
        public int Id { get; set; }

        public string AssigneeId { get; set; }

        public string OwnerId { get; set; }

        public int ProjectScopeId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string SystemId { get; set; }

        public ProjectTaskStatus Status { get; set; }

        public List<string> Tags { get; set; }

        public bool IsFlagged { get; set; }

        public double SortOrder { get; set; }

        public int? ProjectId { get; set; }

        public int? WorkspaceId { get; set; }

        public string WorkspaceKey { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string AssigneeUsername { get; set; }

        public string AssigneePictureUrl { get; set; }

        public string OwnerUsername { get; set; }

        public string OwnerPictureUrl { get; set; }

        public string ProjectName { get; set; }
    }
}
