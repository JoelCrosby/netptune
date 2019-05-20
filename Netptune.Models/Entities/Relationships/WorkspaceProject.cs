namespace Netptune.Entities.Entites.Relationships
{
    public class WorkspaceProject : RelationshipBaseModel
    {

        public int WorkspaceId { get; set; }
        public virtual Workspace Workspace { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

    }
}
