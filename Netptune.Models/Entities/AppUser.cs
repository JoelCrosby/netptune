using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Netptune.Models.Entites.Relationships;
using Newtonsoft.Json;

namespace Netptune.Models.Entites
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PictureUrl { get; set; }

        public string GetDisplayName()
        {
            if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
                return UserName;

            if (string.IsNullOrWhiteSpace(LastName))
                return FirstName;

            return $"{FirstName} {LastName}";
        }

    #region NavigationProperties

        [JsonIgnore]
        public virtual DateTimeOffset? LastLoginTime { get; set; }

        [JsonIgnore]
        public virtual DateTimeOffset?  RegistrationDate { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new List<WorkspaceAppUser>();

        [JsonIgnore]
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; } = new List<WorkspaceProject>();

        [JsonIgnore]
        public virtual ICollection<ProjectUser> ProjectUsers { get; } = new List<ProjectUser>();

        [JsonIgnore]
        public virtual ICollection<ProjectTask> Tasks { get; } = new List<ProjectTask>();

        [JsonIgnore]
        public virtual ProjectTask Assigneee { get; }

    #endregion

    }
}
