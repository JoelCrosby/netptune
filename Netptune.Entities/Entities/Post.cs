using Netptune.Entities.Entites.BaseEntities;
using Netptune.Entities.Enums;
using Newtonsoft.Json;

namespace Netptune.Entities.Entites
{
    public class Post : AuditableEntity<int>
    {
        public string Title { get; set; }

        public string Body { get; set; }

        public PostType Type { get; set; }

        #region ForeignKeys

        public int ProjectId { get; set; }

        #endregion

        #region NavigationProperties

        [JsonIgnore]
        public virtual Project Project { get; set; }

        #endregion

    }
}
