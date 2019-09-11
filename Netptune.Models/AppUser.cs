using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Identity;

using Netptune.Models.Relationships;

using Newtonsoft.Json;

namespace Netptune.Models
{
    public class AppUser : IdentityUser
    {
        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public string PictureUrl { get; set; }

        public string GetDisplayName()
        {
            if (string.IsNullOrWhiteSpace(Firstname) && string.IsNullOrWhiteSpace(Lastname))
                return UserName;

            return $"{Firstname} {Lastname}";
        }

        #region NavigationProperties

        [JsonIgnore]
        public virtual DateTimeOffset? LastLoginTime { get; set; }

        [JsonIgnore]
        public virtual DateTimeOffset? RegistrationDate { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; } = new HashSet<WorkspaceAppUser>();

        [JsonIgnore]
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; set; } = new HashSet<WorkspaceProject>();

        [JsonIgnore]
        public virtual ICollection<ProjectUser> ProjectUsers { get; set; } = new HashSet<ProjectUser>();

        [JsonIgnore]
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new HashSet<ProjectTask>();

        #endregion

    }
}
