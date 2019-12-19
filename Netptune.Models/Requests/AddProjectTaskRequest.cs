﻿using Netptune.Models.Enums;

namespace Netptune.Models.Requests
{
    public class AddProjectTaskRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public ProjectTaskStatus? Status { get; set; }

        public int ProjectId { get; set; }

        public string Workspace { get; set; }

        public AppUser Assignee { get; set; }

        public string AssigneeId { get; set; }
    }
}
