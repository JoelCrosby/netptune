using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netptune.Entities.Entites.BaseEntities
{
    public abstract class AuditableEntity<TValue> : KeyedEntity<TValue>
    {

        public bool IsDeleted { get; set; }

        public byte[] Version { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        //public DateTime DeletedAt { get; set; }

        #region ForeignKeys

        [ForeignKey("CreatedByUser")]
        public string CreatedByUserId { get; set; }

        [ForeignKey("ModifiedByUser")]
        public string ModifiedByUserId { get; set; }

        [ForeignKey("DeletedByUser")]
        public string DeletedByUserId { get; set; }

        [ForeignKey("Owner")]
        public string OwnerId { get; set; }

        #endregion

        #region NavigationProperties

        [JsonIgnore]
        public virtual AppUser CreatedByUser { get; set; }

        [JsonIgnore]
        public virtual AppUser ModifiedByUser { get; set; }


        [JsonIgnore]
        public virtual AppUser DeletedByUser { get; set; }


        [JsonIgnore]
        public virtual AppUser Owner { get; set; }

        #endregion

    }
}
