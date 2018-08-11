using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataPlane.Models.Relationships
{
    public class WorkspaceAppUser : RelationshipBaseModel
    {

        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; }

        public string UserId { get; set; }
        public AppUser User { get; set; }

    }
}
