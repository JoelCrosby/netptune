using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests
{
    public class UpdateProjectRequest
    {
        [Required]
        public int? Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        public string Key { get; set; }
    }
}
