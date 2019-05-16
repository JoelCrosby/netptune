namespace Netptune.Models.Entites.Relationships
{
    public class WorkspaceAppUser : RelationshipBaseModel
    {

        public int WorkspaceId { get; set; }
        public virtual Workspace Workspace { get; set; }

        public string UserId { get; set; }
        public virtual AppUser User { get; set; }

    }
}
