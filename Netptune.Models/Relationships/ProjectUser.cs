using Netptune.Models.BaseEntities;

namespace Netptune.Models.Relationships
{
    public class ProjectUser : KeyedEntity<int>
    {
        public int ProjectId { get; set; }

        public string UserId { get; set; }

        #region NavigationProperties

        public virtual Project Project { get; set; }

        public virtual AppUser User { get; set; }

        #endregion
    }
}
