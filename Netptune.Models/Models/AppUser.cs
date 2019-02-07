using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Netptune.Models.Models.Relationships;

namespace Netptune.Models.Models
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string PictureUrl { get; set; }

        public virtual DateTimeOffset? LastLoginTime { get; set; }
        public virtual DateTimeOffset?  RegistrationDate { get; set; }

        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new List<WorkspaceAppUser>();
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; } = new List<WorkspaceProject>();

        public virtual ICollection<ProjectUser> ProjectUsers { get; } = new List<ProjectUser>();
        public virtual ICollection<ProjectTask> Tasks { get; } = new List<ProjectTask>();

        public virtual ProjectTask Assigneee { get; }

        public string GetDisplayName()
        {
            if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
                return UserName;

            if (string.IsNullOrWhiteSpace(LastName))
                return FirstName;

            return $"{FirstName} {LastName}";
        }
    }
}
