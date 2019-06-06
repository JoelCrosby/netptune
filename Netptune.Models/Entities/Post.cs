using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Netptune.Entities.Entites.BaseEntities;
using Netptune.Entities.Enums;
using Newtonsoft.Json;

namespace Netptune.Entities.Entites
{
    public class Post : AuditableEntity<int>
    {

        [Required]
        [StringLength(128)]
        public string Title { get; set; }

        [StringLength(4096)]
        public string Body { get; set; }

        public PostType Type { get; set; }

        #region ForeignKeys

        [Required]
        [ForeignKey("Project")]
        public int ProjectId { get; set; }

        #endregion

        #region NavigationProperties

        [JsonIgnore]
        public virtual Project Project { get; set; }

        #endregion

    }
}
