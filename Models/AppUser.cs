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

        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new List<WorkspaceAppUser>();
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; } = new List<WorkspaceProject>();

        public virtual ICollection<ProjectUser> ProjectUsers { get; } = new List<ProjectUser>();
        public virtual ICollection<ProjectTask> Tasks { get; } = new List<ProjectTask>();
    }
}
