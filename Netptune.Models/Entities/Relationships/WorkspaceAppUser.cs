using Netptune.Entities.Entites.BaseEntities;

namespace Netptune.Entities.Entites.Relationships
{
    public class WorkspaceAppUser : KeyedEntity<int>
    {
        public int WorkspaceId { get; set; }
        public virtual Workspace Workspace { get; set; }

        public string UserId { get; set; }
        public virtual AppUser User { get; set; }
    }
}
