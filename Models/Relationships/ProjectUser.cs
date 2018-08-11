using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataPlane.Models.Relationships
{
    public class ProjectUser : RelationshipBaseModel
    {
        
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public string UserId { get; set; }
        public AppUser User { get; set; }

    }
}
