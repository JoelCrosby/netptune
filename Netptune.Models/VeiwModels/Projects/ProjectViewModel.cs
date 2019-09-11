namespace Netptune.Models.VeiwModels.Projects
{
    public class ProjectViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        public int WorkspaceId { get; set; }

        public string OwnerDisplayName { get; set; }
    }
}
