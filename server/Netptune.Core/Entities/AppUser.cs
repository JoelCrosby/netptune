using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Identity;

using Netptune.Core.BaseEntities;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.Entities
{
    public class AppUser : IdentityUser, IKeyedEntity<string>
    {
        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public string PictureUrl { get; set; }

        public string DisplayName => GetDisplayName();

        private string GetDisplayName()
        {
            if (string.IsNullOrWhiteSpace(Firstname) && string.IsNullOrWhiteSpace(Lastname))
                return UserName;

            return $"{Firstname} {Lastname}";
        }

        public UserViewModel ToViewModel()
        {
            return new UserViewModel
            {
                Id = Id,
                Firstname = Firstname,
                Lastname = Lastname,
                PictureUrl = PictureUrl,
                DisplayName = DisplayName,
                Email = Email,
                UserName = UserName,
                LastLoginTime = LastLoginTime,
                RegistrationDate = RegistrationDate,
            };
        }

        #region NavigationProperties

        [JsonIgnore]
        public DateTime? LastLoginTime { get; set; }

        [JsonIgnore]
        public DateTime? RegistrationDate { get; set; }

        [JsonIgnore]
        public ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new HashSet<WorkspaceAppUser>();

        [JsonIgnore]
        public ICollection<WorkspaceProject> WorkspaceProjects { get; } = new HashSet<WorkspaceProject>();

        [JsonIgnore]
        public ICollection<ProjectUser> ProjectUsers { get; } = new HashSet<ProjectUser>();

        [JsonIgnore]
        public ICollection<ProjectTask> Tasks { get; } = new HashSet<ProjectTask>();

        [JsonIgnore]
        public ICollection<Workspace> Workspaces { get; } = new HashSet<Workspace>();

        #endregion
    }
}
