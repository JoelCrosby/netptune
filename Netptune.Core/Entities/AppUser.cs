using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Identity;

using Netptune.Core.BaseEntities;
using Netptune.Core.Relationships;

namespace Netptune.Core.Entities
{
    public class AppUser : IdentityUser, IKeyedEntity<string>
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
        public DateTimeOffset? LastLoginTime { get; set; }

        [JsonIgnore]
        public DateTimeOffset? RegistrationDate { get; set; }

        [JsonIgnore]
        public ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new HashSet<WorkspaceAppUser>();

        [JsonIgnore]
        public ICollection<WorkspaceProject> WorkspaceProjects { get; } = new HashSet<WorkspaceProject>();

        [JsonIgnore]
        public ICollection<ProjectUser> ProjectUsers { get; } = new HashSet<ProjectUser>();

        [JsonIgnore]
        public ICollection<ProjectTask> Tasks { get; } = new HashSet<ProjectTask>();

        #endregion

    }
}
