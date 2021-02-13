using System.ComponentModel.DataAnnotations;

using Netptune.Core.Enums;

namespace Netptune.Core.Models.Activity
{
    public class ActivityOptions
    {
        [Required]
        public int? EntityId { get; set; }

        [Required]
        public int? WorkspaceId { get; set; }

        [Required]
        public EntityType EntityType { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public ActivityType Type { get; set; }
    }
}
