using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;

namespace Netptune.Core.Requests
{
    public class UpdateProjectTaskRequest
    {
        [Required]
        public int? Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ProjectTaskStatus? Status { get; set; }

        public bool? IsFlagged { get; set; }

        public double? SortOrder { get; set; }

        public string OwnerId { get; set; }

        public string AssigneeId { get; set; }
    }
}
