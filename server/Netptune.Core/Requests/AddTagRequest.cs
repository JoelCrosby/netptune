using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests
{
    public class AddTagRequest
    {
        [Required]
        public string Tag { get; set; }

        [Required]
        public string SystemId { get; set; }

        [Required]
        public string WorkspaceSlug { get; set; }
    }
}
