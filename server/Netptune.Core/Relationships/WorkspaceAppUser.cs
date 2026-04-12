using System.Text.Json.Serialization;

using Netptune.Core.Authorization;
using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.Relationships;

public record WorkspaceAppUser : KeyedEntity<int>
{
    public int WorkspaceId { get; set; }

    public string UserId { get; set; } = null!;

    public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;

    public List<string> Permissions { get; set; } = null!;

    #region NavigationProperties

    [JsonIgnore]
    public Workspace Workspace { get; set; } = null!;

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion

    public WorkspaceUserViewModel ToWorkspaceViewModel()
    {
        return new WorkspaceUserViewModel
        {
            Role = Role,
            DisplayName = User.DisplayName,
            Email = User.Email,
            Firstname = User.Firstname,
            Lastname = User.Lastname,
            Id = User.Id,
            LastLoginTime = User.LastLoginTime,
            PictureUrl = User.PictureUrl,
            RegistrationDate = User.RegistrationDate,
            UserName = User.UserName,
        };
    }
}
