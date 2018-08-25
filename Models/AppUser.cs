using DataPlane.Models.Relationships;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace DataPlane.Models
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string PictureUrl { get; set; }

        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; }
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; set; }

        public virtual ICollection<ProjectUser> ProjectUsers { get; set; }
        public virtual ICollection<Task> Tasks { get; set; }
    }
}
