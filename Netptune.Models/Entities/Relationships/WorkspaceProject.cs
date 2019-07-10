using Netptune.Entities.Entites.BaseEntities;

namespace Netptune.Entities.Entites.Relationships
{
    public class WorkspaceProject : KeyedEntity<int>
    {
        public int WorkspaceId { get; set; }
        public virtual Workspace Workspace { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
    }
}
