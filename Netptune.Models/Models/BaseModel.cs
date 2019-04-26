using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Netptune.Models.Interfaces;
using Newtonsoft.Json;

namespace Netptune.Models.Models
{
    public abstract class BaseModel : IBaseEntity
    {

        // Primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool IsDeleted { get; set; }

        [Timestamp]
        public byte[] Version { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

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
