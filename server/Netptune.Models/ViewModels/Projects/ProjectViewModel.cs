namespace Netptune.Models.ViewModels.Projects
{
    public class ProjectViewModel
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }

        public required string RepositoryUrl { get; set; }

        public int WorkspaceId { get; set; }

        public int? DefaultStatusId { get; set; }

        public required string OwnerDisplayName { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
