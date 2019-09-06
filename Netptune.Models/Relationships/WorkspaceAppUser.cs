using Netptune.Models.BaseEntities;

namespace Netptune.Models.Relationships
{
    public class WorkspaceAppUser : KeyedEntity<int>
    {
        public int WorkspaceId { get; set; }

        public string UserId { get; set; }

        #region NavigationProperties

        public virtual Workspace Workspace { get; set; }

        public virtual AppUser User { get; set; }

        #endregion
    }
}
