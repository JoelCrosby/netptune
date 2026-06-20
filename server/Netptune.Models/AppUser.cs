using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Identity;

using Netptune.Models.Relationships;

namespace Netptune.Models
{
    public class AppUser : IdentityUser
    {
        public required string Firstname { get; set; }

        public required string Lastname { get; set; }

        public required string PictureUrl { get; set; }

        public string GetDisplayName()
        {
            if (string.IsNullOrWhiteSpace(Firstname) && string.IsNullOrWhiteSpace(Lastname))
                return UserName ?? Email ?? throw new Exception("name not found");

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
