using System.ComponentModel.DataAnnotations;

namespace Netptune.Models.Requests
{
    public class AddProjectRequest
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        [Required]
        public string Workspace { get; set; }
    }
}
