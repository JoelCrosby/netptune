using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

using System.Text.Json.Serialization;

namespace Netptune.Core
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
