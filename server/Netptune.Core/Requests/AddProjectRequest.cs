using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests
{
    public class AddProjectRequest
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        public string Key { get; set; }
    }
}
