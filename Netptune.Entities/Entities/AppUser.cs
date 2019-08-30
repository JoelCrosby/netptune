using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Netptune.Entities.Entites.Relationships;
using Newtonsoft.Json;

namespace Netptune.Entities.Entites
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

            return $"{FirstName} {LastName}";
        }

        #region NavigationProperties

        [JsonIgnore]
        public virtual DateTimeOffset? LastLoginTime { get; set; }

        [JsonIgnore]
        public virtual DateTimeOffset? RegistrationDate { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProjectUser> ProjectUsers { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProjectTask> Tasks { get; set; }

        #endregion

    }
}
